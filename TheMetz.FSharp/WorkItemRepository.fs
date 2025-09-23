namespace TheMetz.FSharp.WorkItemRepository

open System.Text.Json
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models
open Microsoft.VisualStudio.Services.WebApi
open TheMetz.Interfaces

type WorkItemRepository
    (connection: VssConnection, teamService: ITeamMemberService) =
    let ConnectionString = "Data Source=../../../TheMetz.db"
    
    let ReadWorkItems (reader: SqliteDataReader) =
        task{
            let workItems = System.Collections.Generic.List<WorkItem>()
            let rec readAll () = task {
                let! hasRow = reader.ReadAsync()
                if hasRow then
                    let workItem = JsonSerializer.Deserialize<WorkItem>(reader.GetString(0))
                    if not (isNull workItem) then
                        workItems.Add(workItem)
                    return! readAll ()
                else
                    return ()
            }
        
            do! readAll ()

            return workItems
        }
        
    let ReadWorkItem (reader: SqliteDataReader) =
        task{
            let! hasRow = reader.ReadAsync()
            if hasRow then
                let workItem = JsonSerializer.Deserialize<WorkItem>(reader.GetString(0))
                if not (isNull workItem) then
                    return Some workItem
                else return None
            else
                return None
        }
        
    interface IWorkItemRepository with
        member this.AddWorkItem (workItemJson: string): Task =
            task {
                use connection = new SqliteConnection(ConnectionString)
    
                do! connection.OpenAsync() |> Async.AwaitTask

                let command = connection.CreateCommand()
                command.CommandText = @"
                        INSERT INTO main.WorkItems (Data) VALUES ($workItemJson)
                    " |> ignore
                
                command.Parameters.AddWithValue("workItemJson", workItemJson) |> ignore
                
                return command.ExecuteNonQueryAsync()
            }

        member this.GetAllWorkItems() =
            task {
                use connection = new SqliteConnection(ConnectionString)
    
                do! connection.OpenAsync() |> Async.AwaitTask

                let command = connection.CreateCommand()
                command.CommandText = @"
                        SELECT wi.Data FROM main.WorkItems wi
                    " |> ignore;
                
                use! reader = command.ExecuteReaderAsync()
                
                let! workItems = ReadWorkItems reader
                return workItems
            }
            
        member this.GetLatestClosedWorkItem() =
            task {
                use connection = new SqliteConnection(ConnectionString)
        
                do! connection.OpenAsync()

                let command = connection.CreateCommand()
                command.CommandText = @"
                        SELECT wi.* FROM main.WorkItems wi
                        ORDER BY json_extract(wi.Data, '$.ClosedDate') DESC
                        LIMIT 1;
                    "
                |> ignore;
                
                use! reader = command.ExecuteReaderAsync()
                
                let! workItem = ReadWorkItem reader
                match workItem with
                | Some workItem -> return workItem
                | _ -> return null                 
            }
            
        member this.GetLatestCreatedWorkItem() =
            task{
                use connection = new SqliteConnection(ConnectionString)
        
                do! connection.OpenAsync()

                let command = connection.CreateCommand()
                command.CommandText = @"
                        SELECT wi.* FROM main.WorkItems wi
                        ORDER BY json_extract(wi.Data, '$.CreationDate') DESC
                        LIMIT 1;
                    "
                |> ignore;
                
                use! reader = command.ExecuteReaderAsync()
                
                let! workItem = ReadWorkItem reader
                match workItem with
                | Some workItem -> return workItem
                | _ -> return null                 
            }
        
        member this.GetWorkItemsByDateCreated dateCreated =
            task{
                use connection = new SqliteConnection(ConnectionString)
                
                do! connection.OpenAsync()

                let command = connection.CreateCommand()
                command.CommandText = @"
                        SELECT wi.Data FROM main.WorkItems wi
                        WHERE DATETIME(json_extract(wi.Data, '$.CreationDate')) >= DATETIME($dateCreated)
                    "
                |> ignore

                command.Parameters.AddWithValue("dateCreated", dateCreated.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss")) |> ignore
                
                use! reader = command.ExecuteReaderAsync()
                
                let! workItems = ReadWorkItems reader
                return workItems
            }
