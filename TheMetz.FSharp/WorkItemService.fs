namespace TheMetz.FSharp.WorkItemService

open System
open System.Threading.Tasks
open Microsoft.TeamFoundation.WorkItemTracking.WebApi
open Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models
open Microsoft.VisualStudio.Services.WebApi
open TheMetz.FSharp.Models
open TheMetz.Interfaces

type WorkItemService
    (connection: VssConnection, workItemRepository: IWorkItemRepository, teamService: ITeamMemberService) =
    
    let mutable workItems: WorkItemInfo list = []

    let GenerateWiqlByProject (projectName: string) (developerDisplayName: string) (developerEmail:string) (startDate:string) =
        let query = $@"
                     SELECT [System.Id], [System.Title], [System.AssignedTo]
                     FROM WorkItems 
                     WHERE [System.TeamProject] = '{projectName}' 
                     AND [System.ChangedDate] >= '{startDate}'
                     AND ([System.State] = 'Active' OR ([System.State] = 'Done' AND [Microsoft.VSTS.Common.ClosedDate] >= '{startDate}'))
                     AND (
                         [System.History] CONTAINS '{developerDisplayName}'
                         OR [System.History] CONTAINS '{developerEmail}'
                         OR [System.History] CONTAINS '@{developerDisplayName}'
                         OR [System.ChangedBy] = '{developerDisplayName}'
                         OR [System.AssignedTo] = '{developerDisplayName}'
                         OR [System.CreatedBy] = '{developerDisplayName}'
                     )
                     ORDER BY [System.ChangedDate] DESC"

        Wiql(Query = query)

    let QueryByProject (workItemClient:WorkItemTrackingHttpClient) startDate (teamMember: TeamMember)  projectName=
        task{
            let wiql = GenerateWiqlByProject projectName teamMember.Identity.DisplayName teamMember.Identity.UniqueName startDate
        
            let! result = workItemClient.QueryByWiqlAsync(wiql)
            
            if result.WorkItems |> Seq.length = 0 then
                return ()
            else
                
            let workItemIds = result.WorkItems |> Seq.map (fun wi -> wi.Id) |> Seq.distinct |> Seq.toList
                        
            let! workItemsArrays =
                [ for i in 0 .. 200 .. (workItemIds.Length - 1) ->
                    task {
                        let batchIds =
                            workItemIds
                            |> Seq.skip i
                            |> Seq.truncate 200
                            |> Seq.toArray
                        let! batch =
                            workItemClient.GetWorkItemsAsync(
                                ids = batchIds,
                                expand = Nullable(WorkItemExpand.All)
                            )
                        return batch |> Seq.toList
                    } ]
                |> Task.WhenAll

            let projectWorkItems = workItemsArrays |> List.concat

            match workItems |> List.tryFind (fun wi -> wi.DeveloperName = teamMember.Identity.DisplayName) with
            | Some existing -> existing.WorkItems <- Seq.append (List.toSeq projectWorkItems) existing.WorkItems
            | None ->
                let newEntry: WorkItemInfo =
                    { DeveloperName = teamMember.Identity.DisplayName
                      WorkItems = projectWorkItems |> Seq.ofList }
                workItems <- workItems @ [ newEntry ]
            
            return ()
        }

    let QueryByTeamMember (workItemClient:WorkItemTrackingHttpClient) startDate (teamMember: TeamMember) =
        task{
            let projects = [ "Marketplace"; "Atlas"; "Concorde"; "Specialty Channel Development" ]
            let projectQuery = QueryByProject workItemClient startDate teamMember

            let! workItemsArrays =
                    [ for i in 0 .. (projects.Length - 1) ->
                        projectQuery projects.[i] ]
                    |> Task.WhenAll
            return ()
        }
        
        
    interface IWorkItemService with
        member this.GetWorkItemsCompletedByDevLinks (devName: string) =
            workItems |> List.find (fun wi -> wi.DeveloperName = devName)

        member this.GetWorkItemsWithDeveloperReferenceSinceDate(sinceDate) =
            task{
                let! workItemClient = connection.GetClientAsync<WorkItemTrackingHttpClient>()

                let startDate = sinceDate.ToString("yyyy-MM-dd")

                let! teamMembersEnumerable = teamService.GetCustomerOptimizationTeamMembers()
                let teamMembers = teamMembersEnumerable |> Seq.toList
                
                let teamQuery = QueryByTeamMember workItemClient startDate
                let! workItemsArrays =
                    [ for i in 0 .. (teamMembers.Length - 1) ->
                        teamQuery teamMembers.[i] ]
                    |> Task.WhenAll
                return workItems |> Seq.ofList
            }
