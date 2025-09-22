namespace TheMetz.FSharp

open System
open System.Collections.Concurrent
open System.Linq
open Microsoft.TeamFoundation.SourceControl.WebApi
open Microsoft.VisualStudio.Services.WebApi
open TheMetz.FSharp.Models
open TheMetz.Interfaces

type PullRequestCommentServiceF
    (connection: VssConnection, pullRequestService: IPullRequestService, teamService: ITeamMemberService) =
    let developerCommentLinks = ConcurrentDictionary<string, Link list>()

    let GetFormattedPrUrl (pr: GitPullRequest) =
        $"https://tfs.clarkinc.biz/DefaultCollection/{pr.Repository.ProjectReference.Name}/_git/{pr.Repository.Name}/pullrequest/{pr.PullRequestId}"

    interface ICommentService with
        member this.GetDeveloperCommentLinks developerName =
            System.Collections.Generic.List(developerCommentLinks.[developerName])

        member this.ShowCommentCounts numberOfDays =
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

                let perAuthorTotalReviews = dict<string, int>
                // foreach (string memberName in memberNames)
                // {
                //     perAuthorTotalReviews.Add(memberName, filteredPullRequests.Count(pr =>
                //         pr.Reviewers.Any(r => memberName == r.DisplayName) && pr.CreatedBy.DisplayName != memberName));
                // }
                //
                // using var gitClient = await _connection.GetClientAsync<GitHttpClient>();
                //
                // await Parallel.ForEachAsync(filteredPullRequests, new ParallelOptions
                // {
                //     MaxDegreeOfParallelism = Environment.ProcessorCount // Adjust as needed
                // }, async (pr, cancellationToken) =>
                // {
                //     try
                //     {
                //         List<GitPullRequestCommentThread> threads = (await gitClient.GetThreadsAsync(
                //             repositoryId: pr.Repository.Id,
                //             pullRequestId: pr.PullRequestId,
                //             cancellationToken: cancellationToken
                //         )).ToList();
                //
                //         List<string> authorComments = threads.SelectMany(t => t.Comments)
                //             .Where(c => c.CommentType != CommentType.System && c.PublishedDate >= fromDate)
                //             .GroupBy(c => c.Author.DisplayName)
                //             .Select(c => c.Key).ToList();
                //
                //         foreach (string? authorComment in authorComments)
                //         {
                //             if (authorComment == pr.CreatedBy.DisplayName
                //                 || !memberNames.Contains(authorComment))
                //             {
                //                 continue;
                //             }
                //
                //             if (_developerCommentLinks.TryGetValue(authorComment,
                //                     out List<(string Title, string Url)>? value)
                //                 && value.Any(r => r.Title == pr.Title))
                //             {
                //                 continue;
                //             }
                //
                //             developerCommentCount.AddOrUpdate(
                //                 authorComment,
                //                 (perAuthorTotalReviews[authorComment], 1),
                //                 (string _, (int totalReviews, int commentCount)count) => (count.totalReviews, count.commentCount + 1)
                //             );
                //             _developerCommentLinks.AddOrUpdate(
                //                 authorComment,
                //                 _ => new List<(string, string)> { (pr.Title, GetFormattedPrUrl(pr)) },
                //                 (_, existingList) =>
                //                 {
                //                     existingList.Add((pr.Title, GetFormattedPrUrl(pr)));
                //                     return existingList;
                //                 }
                //             );
                //         }
                //     }
                //     catch (Exception e)
                //     {
                //         Console.WriteLine(e);
                //     }
                // });
                //
                return developerCommentCount.ToDictionary()
            }
