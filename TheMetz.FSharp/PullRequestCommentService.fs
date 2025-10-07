namespace TheMetz.FSharp

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Linq
open System.Threading
open System.Threading.Tasks
open Microsoft.TeamFoundation.SourceControl.WebApi
open Microsoft.VisualStudio.Services.WebApi
open TheMetz.FSharp.Models
open TheMetz.Interfaces

type PullRequestCommentService
    (connection: VssConnection, pullRequestService: IPullRequestService, teamService: ITeamMemberService) =
    let developerCommentLinks = ConcurrentDictionary<string, Link list>()

    let GetFormattedPrUrl (pr: GitPullRequest) =
        $"https://tfs.clarkinc.biz/DefaultCollection/{pr.Repository.ProjectReference.Name}/_git/{pr.Repository.Name}/pullrequest/{pr.PullRequestId}"

    let CalculateDeveloperTotalReviews (filteredPullRequests: GitPullRequest seq) (teamMembers: string seq) =
        teamMembers
        |> Seq.map (fun m ->
            m,
            filteredPullRequests
            |> Seq.sumBy (fun pr ->
                if
                    (pr.CreatedBy.DisplayName <> m
                     && pr.Reviewers |> Seq.exists (fun r -> m = r.DisplayName))
                then
                    1
                else
                    0))
        |> dict

    let UpdateDevInfo (perAuthorTotalReviews: IDictionary<string, int>) (developerCommentCount: ConcurrentDictionary<string, ReviewCounts>) (authorWithComment: string) (pr: GitPullRequest)  =
        developerCommentCount.AddOrUpdate(
            authorWithComment,
            (fun _ -> { TotalReviews = perAuthorTotalReviews.[authorWithComment]; ReviewsWithComments = 1 }),
            (fun _ existing -> { existing with ReviewsWithComments = existing.ReviewsWithComments + 1 })
        ) |> ignore
        
        developerCommentLinks.AddOrUpdate(
            authorWithComment,
            (fun _ -> [ { Title = pr.Title; Url = GetFormattedPrUrl(pr)}]),
            (fun _ existing -> existing @ [{Title = pr.Title; Url = GetFormattedPrUrl(pr) } ])
        ) |> ignore
            
    interface IPullRequestCommentService with
        member this.GetDeveloperCommentLinks developerName =
            List(developerCommentLinks.[developerName])
            
        member this.ShowCommentCounts numberOfDays =
            developerCommentLinks.Clear();
            let fromDate = DateTime.Today.AddDays(-numberOfDays)

            task {
                let! allTeamMembers = teamService.GetCustomerOptimizationTeamMembers()
                let memberNames = ConcurrentBag<string>()
                allTeamMembers |> Seq.iter (fun t -> memberNames.Add(t.Identity.DisplayName))

                let! pullRequests = pullRequestService.GetPullRequestsByDateOpenedOrClosed(numberOfDays)

                let developerCommentCount = ConcurrentDictionary<string, ReviewCounts>()

                let filteredPullRequests =
                    pullRequests
                    |> Seq.filter (fun (pr: GitPullRequest) ->
                        pr.SourceRefName <> "refs/heads/develop"
                        && pr.SourceRefName <> "refs/heads/Test"
                        && pr.CreatedBy.DisplayName
                           <> "Project Collection BuildService (Default Collection)"
                        && pr.Reviewers
                           |> Seq.exists (fun r ->
                               allTeamMembers
                               |> Seq.map (fun t -> t.Identity.DisplayName)
                               |> Seq.contains r.DisplayName))

                let perAuthorTotalReviews = CalculateDeveloperTotalReviews filteredPullRequests memberNames

                let updateDevInfoFunc = UpdateDevInfo perAuthorTotalReviews developerCommentCount

                use! gitClient = connection.GetClientAsync<GitHttpClient>() |> Async.AwaitTask

                do!
                    Parallel.ForEachAsync(
                        filteredPullRequests,
                        ParallelOptions(MaxDegreeOfParallelism = Environment.ProcessorCount),
                        Func<_, _, _>(fun (pr: GitPullRequest) (ct: CancellationToken) ->
                            task {
                                let! threads =
                                    gitClient.GetThreadsAsync(
                                        repositoryId = pr.Repository.Id,
                                        pullRequestId = pr.PullRequestId,
                                        cancellationToken = ct
                                    )

                                let authorsWithComments =
                                    threads
                                    |> Seq.collect (fun t -> t.Comments)
                                    |> Seq.filter (fun c ->
                                        c.CommentType <> CommentType.System && c.PublishedDate >= fromDate)
                                    |> Seq.groupBy (fun c -> c.Author.DisplayName)
                                    |> Seq.map fst
                                    |> Seq.filter (fun awc ->
                                        awc <> pr.CreatedBy.DisplayName
                                        && memberNames |> Seq.contains awc
                                        && ((developerCommentLinks.ContainsKey(awc)
                                             |> not)
                                                || developerCommentLinks.[awc] |> (fun values ->
                                                values |> Seq.exists (fun x -> x.Title = pr.Title)
                                                |> not)
                                            )
                                        )

                                authorsWithComments |> Seq.iter (fun awc -> updateDevInfoFunc awc pr)

                                return ()
                            }
                            |> fun t -> ValueTask t)
                    )
                    
                return developerCommentCount.ToDictionary()
            }
