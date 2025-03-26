using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace TheMetz.Repositories;

public interface IWorkItemRepository
{
    Task AddWorkItem(string workItemJson);
    Task<List<WorkItem>> GetAllWorkItems();
    Task<List<WorkItem>> GetWorkItemsByDateCreated(DateTime dateCreated);
    Task<WorkItem?> GetLatestCreatedWorkItem();
    Task<WorkItem?> GetLatestClosedWorkItem();
}

public class WorkItemRepository : IWorkItemRepository
{
    private readonly string _connectionString;

    public WorkItemRepository()
    {
        // Specify the path to your SQLite database file
        _connectionString = "Data Source=TheMetz.db"; 
    }
    
    public async Task AddWorkItem(string workItemJson)
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                INSERT INTO main.WorkItems (Data) VALUES ($workItemJson)
            ";
        command.Parameters.AddWithValue("workItemJson", workItemJson);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<WorkItem>> GetAllWorkItems()
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT wi.Data FROM main.WorkItems wi
            ";
        
        var workItems = new List<WorkItem>();

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var workItem = JsonSerializer.Deserialize<WorkItem>(reader.GetString(0));
            if (workItem != null)
            {
                workItems.Add(workItem);
            }
        }

        return workItems;
    }

    public async Task<List<WorkItem>> GetWorkItemsByDateCreated(DateTime dateCreated)
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT wi.Data FROM main.WorkItems wi
                WHERE DATETIME(json_extract(wi.Data, '$.CreationDate')) >= DATETIME($dateCreated)
            ";

        command.Parameters.AddWithValue("dateCreated", dateCreated.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss"));
        
        var workItems = new List<WorkItem>();

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var workItem = JsonSerializer.Deserialize<WorkItem>(reader.GetString(0));
            if (workItem != null)
            {
                workItems.Add(workItem);
            }
        }

        return workItems;
    }
    
    public async Task<WorkItem?> GetLatestCreatedWorkItem()
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT wi.* FROM main.WorkItems wi
                ORDER BY json_extract(wi.Data, '$.CreationDate') DESC
                LIMIT 1;
            ";
        
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var workItem = JsonSerializer.Deserialize<WorkItem>(reader.GetString(1));
            if (workItem != null)
            {
                return workItem;
            }
        }

        return null;
    }
    
    public async Task<WorkItem?> GetLatestClosedWorkItem()
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT wi.* FROM main.WorkItems wi
                ORDER BY json_extract(wi.Data, '$.ClosedDate') DESC
                LIMIT 1;
            ";
        
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var workItem = JsonSerializer.Deserialize<WorkItem>(reader.GetString(1));
            if (workItem != null)
            {
                return workItem;
            }
        }

        return null;
    }
}