namespace TheMetz.FSharp

open System.Threading.Tasks
open Microsoft.TeamFoundation.Core.WebApi
open Microsoft.VisualStudio.Services.WebApi
open TheMetz.Interfaces

type TeamMemberService(connection: VssConnection) =
    interface ITeamMemberService with
        member _.GetCustomerOptimizationTeamMembers() : Task<System.Collections.Generic.List<TeamMember>> =
            task {
                let! teamClient = connection.GetClientAsync<TeamHttpClient>();
                    
                let! members = teamClient.GetTeamMembersWithExtendedPropertiesAsync(
                    projectId = "Marketplace",
                    teamId = "Customer Optimization"
                )
                
                return members
            }
