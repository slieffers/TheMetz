namespace TheMetz.FSharp

open System
open System.Linq
open System.Threading.Tasks
open Microsoft.TeamFoundation.Core.WebApi
open Microsoft.TeamFoundation.SourceControl.WebApi
open Microsoft.VisualStudio.Services.WebApi
open TheMetz.Interfaces

type TeamService(connection: VssConnection) =
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

    member _.LoadProjectInfo: Task<(string * (string * Guid) list) list> =
        let projectInfoTemplate: (string * (string * Guid) list) list =
            getProjectInfoTemplate

        task {
            let tasks = projectInfoTemplate |> List.map FetchProjectRepos

            let! projectsWithRepos = Task.WhenAll(tasks)

            return projectsWithRepos |> Array.toList
        }        
        
    interface ITeamMemberService with
        member _.GetCustomerOptimizationTeamMembers() : Task<TeamMember seq> =
            task {
                let! teamClient = connection.GetClientAsync<TeamHttpClient>();
                    
                let! members = teamClient.GetTeamMembersWithExtendedPropertiesAsync(
                    projectId = "Marketplace",
                    teamId = "Customer Optimization"
                )
                
                return members.AsEnumerable()
            }
