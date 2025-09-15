namespace TheMetz.FSharp

open System
open System.Text.Json
open System.Threading.Tasks
open Microsoft.TeamFoundation.SourceControl.WebApi
open Microsoft.VisualStudio.Services.WebApi
open TheMetz.Interfaces
open FSharp.Control

type PullRequestService(connection: VssConnection, prRepository: IPrRepository, prCutoffDate: DateTime) =

    let FetchProjectRepos (repoInfo: string * (string * Guid) list) =
        task {
            let! gitClient = connection.GetClientAsync<GitHttpClient>()

            let projectName = repoInfo |> fst

            let! result = gitClient.GetRepositoriesAsync(repoInfo |> fst)

            let repos =
                result
                |> Seq.map (fun r -> r.Name, r.Id)
                |> Seq.filter (fun (name, id) -> repoInfo |> snd |> List.exists (fun (repoName, _) -> name = repoName))
                |> Seq.toList

            return (projectName, repos)
        }

    let getProjectInfoTemplate =
        [ ("Marketplace",
           [ ("Marketplace", Guid.Empty)
             ("MarketplaceIDSConsumers", Guid.Empty)
             ("Clark Chains", Guid.Empty)
             ("Marketplace Customer Configuration Consumers", Guid.Empty)
             ("ClarkChainsDatabase", Guid.Empty)
             ("Marketplace-react-app", Guid.Empty)
             ("MarketplaceFrontends", Guid.Empty) ])
          ("Concorde",
           [ ("Concorde", Guid.Empty)
             ("Clark.Concorde.Models", Guid.Empty)
             ("ConcordeConsumers", Guid.Empty)
             ("ConcordeDatabase", Guid.Empty) ])
          ("Atlas",
           [ ("Atlas", Guid.Empty)
             ("Atlas.SalesHistoryModal", Guid.Empty)
             ("AtlasConsumers", Guid.Empty)
             ("AtlasDatabase", Guid.Empty) ])
          ("Specialty Channel Development",
           [ ("SpecChannelAirport", Guid.Empty)
             ("SpecChannelAirportDatabase", Guid.Empty)
             ("SpecChannelAirportConsumers", Guid.Empty) ]) ]

    let LoadProjectInfo: Task<(string * (string * Guid) list) list> =
        let projectInfoTemplate: (string * (string * Guid) list) list =
            getProjectInfoTemplate

        task {
            let tasks = projectInfoTemplate |> List.map FetchProjectRepos

            let! projectsWithRepos = Task.WhenAll(tasks)

            return projectsWithRepos |> Array.toList
        }


    let ExtractAndStoreCreatedPullRequests prCreatedCutoffDate (paginatedPullRequests: seq<GitPullRequest>) : Task =
        task {
            let tasks =
                paginatedPullRequests
                |> Seq.filter (fun (pr: GitPullRequest) -> pr.CreationDate > prCreatedCutoffDate)
                |> Seq.map (fun pr -> JsonSerializer.Serialize(pr))
                |> Seq.map prRepository.AddPullRequest
                |> Seq.toArray

            do! Task.WhenAll(tasks)
        }

    let GetAndUpdatePullRequest (gitClient: GitHttpClient) (pullRequest: GitPullRequest) : Task =
        task {
            let! adoPr = gitClient.GetPullRequestByIdAsync(pullRequest.PullRequestId)

            if not (isNull adoPr) then
                do!
                    prRepository.UpdatePullRequestByADOPullRequestId(
                        adoPr.PullRequestId,
                        JsonSerializer.Serialize(adoPr)
                    )
        }

    let GetAndStoreADOPullRequests (gitClient: GitHttpClient) (projectName: string) prOpenCutoffDate prCreatedCutoffDate (openIds: Set<int>) =
        task {
            let pageSize = 100

            let rec loop skip =
                task {
                    let criteria =
                        GitPullRequestSearchCriteria(
                            Status = PullRequestStatus.All,
                            IncludeLinks = true,
                            MinTime = Nullable prOpenCutoffDate
                        )

                    let! page =
                        gitClient.GetPullRequestsByProjectAsync(
                            project = projectName,
                            searchCriteria = criteria,
                            top = Nullable pageSize,
                            skip = Nullable skip
                        )

                    let prs = page |> Seq.toList

                    if prs.Length > 0 then
                        let toStore = prs |> List.filter (fun pr -> not (openIds.Contains pr.PullRequestId))
                        do! ExtractAndStoreCreatedPullRequests prCreatedCutoffDate toStore

                        if prs.Length = pageSize then
                            return! loop (skip + pageSize)
                        else
                            return ()
                    else
                        return ()
                }

            do! loop 0
        }

    let GetAndUpdatePullRequest (gitClient:GitHttpClient) (pullRequest: GitPullRequest) : Task=
        task {
            let! adoPr = gitClient.GetPullRequestByIdAsync(pullRequest.PullRequestId)
            if not (isNull adoPr) then
                do! prRepository.UpdatePullRequestByADOPullRequestId(adoPr.PullRequestId, JsonSerializer.Serialize(adoPr))
        }
        
    let StorePullRequestsFromDate prCreatedCutoffDate prOpenCutoffDate : Task =
        task {
            let! gitClient = connection.GetClientAsync<GitHttpClient>()

            let! result = prRepository.GetOpenPullRequests()
            let openPullRequests = result |> Seq.toList
            let openIds =
                openPullRequests
                |> Seq.map (fun pr -> pr.PullRequestId)
                |> Set.ofSeq

            let openPullRequestsTaskList = Seq.map (GetAndUpdatePullRequest gitClient) openPullRequests
            do! Task.WhenAll(openPullRequestsTaskList)

            let! projectInfo = LoadProjectInfo
            let projectNames = projectInfo |> List.map fst

            for projectName in projectNames do
                do! GetAndStoreADOPullRequests gitClient projectName prOpenCutoffDate prCreatedCutoffDate openIds
        }

    interface IPullRequestService with
        member _.GetPullRequestsByDateOpened
            (numberOfDaysAgoOpened: int)
            : Task<Collections.Generic.List<GitPullRequest>> =
            let openedFromDate = DateTime.UtcNow.AddDays(float (-numberOfDaysAgoOpened))

            if openedFromDate < prCutoffDate then
                invalidArg
                    "numberOfDaysAgoOpened"
                    "Cannot get pull requests before 2024-01-01 without manually changing the cutoff date."

            prRepository.GetPullRequestsByDateOpened(openedFromDate)

        member _.GetPullRequestsByDateClosed
            (numberOfDaysAgoClosed: int)
            : Task<Collections.Generic.List<GitPullRequest>> =
            let closedFromDate = DateTime.UtcNow.AddDays(float (-numberOfDaysAgoClosed))

            if closedFromDate < prCutoffDate then
                invalidArg
                    "numberOfDaysAgoClosed"
                    "Cannot get pull requests before 2024-01-01 without manually changing the cutoff date."

            prRepository.GetPullRequestsByDateClosed(closedFromDate)

        member _.GetPullRequestsByDateOpenedOrClosed
            (numberOfDaysAgoOpened: int, numberOfDaysAgoClosed: int)
            : Task<Collections.Generic.List<GitPullRequest>> =
            let openedFromDate = DateTime.UtcNow.AddDays(float (-numberOfDaysAgoOpened))

            let closedFromDate =
                if numberOfDaysAgoClosed = 0 then
                    openedFromDate
                else
                    DateTime.UtcNow.AddDays(float (-numberOfDaysAgoClosed))

            if openedFromDate < prCutoffDate || closedFromDate < prCutoffDate then
                invalidArg
                    "numberOfDays"
                    "Cannot get pull requests before 2024-01-01 without manually changing the cutoff date."

            prRepository.GetPullRequestsByDateOpenedOrClosed(openedFromDate, closedFromDate)

        member _.UpdateAdoPullRequests() : Task =
            task {
            let! latestCreatedPr = prRepository.GetLatestCreatedPullRequest()
            let latestCreatedPrTuple = latestCreatedPr.ToTuple()
            let! oldestOpenPr = prRepository.GetOldestOpenPullRequest()

            if (latestCreatedPrTuple |> snd |> isNull) then
                do! StorePullRequestsFromDate prCutoffDate prCutoffDate
            else
                let latestResultCreationDateTime = latestCreatedPrTuple |> fst
                let oldestResultOpenDateTime = oldestOpenPr.CreationDate
                do! StorePullRequestsFromDate latestResultCreationDateTime oldestResultOpenDateTime
            }