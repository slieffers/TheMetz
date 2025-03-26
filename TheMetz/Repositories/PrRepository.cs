using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace TheMetz.Repositories;

public interface IPrRepository
{
    Task AddPullRequest(string prJson);
    Task<List<GitPullRequest>> GetAllPullRequests();
    Task<List<GitPullRequest>> GetPullRequestsByDate(DateTime dateOpened, DateTime dateClosed = default);
    Task<GitPullRequest?> GetLatestCreatedPullRequest();
    Task<GitPullRequest?> GetLatestClosedPullRequest();
}

public class PrRepository : IPrRepository
{
    private readonly string _connectionString;

    public PrRepository()
    {
        // Specify the path to your SQLite database file
        _connectionString = "Data Source=TheMetz.db"; 
    }
    
    public async Task AddPullRequest(string prJson)
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                INSERT INTO main.PullRequests (Data) VALUES ($prJson)
            ";
        command.Parameters.AddWithValue("$prJson", prJson);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<GitPullRequest>> GetAllPullRequests()
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT pr.Data FROM main.PullRequests pr
            ";
        
        var pullRequests = new List<GitPullRequest>();

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var pullRequest = JsonSerializer.Deserialize<GitPullRequest>(reader.GetString(0));
            if (pullRequest != null)
            {
                pullRequests.Add(pullRequest);
            }
        }

        return pullRequests;
    }

    public async Task<List<GitPullRequest>> GetPullRequestsByDate(DateTime dateOpened, DateTime dateClosed = default)
    {
        if (dateClosed == default)
        {
            dateClosed = dateOpened;
        }
        
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT pr.Data FROM main.PullRequests pr
                WHERE DATETIME(json_extract(pr.Data, '$.CreationDate')) >= DATETIME($dateOpened)
                    OR DATETIME(json_extract(pr.Data, '$.ClosedDate')) >= DATETIME($dateClosed)

            ";

        command.Parameters.AddWithValue("$dateOpened", dateOpened.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss"));
        command.Parameters.AddWithValue("$dateClosed", dateClosed.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss"));
        
        var pullRequests = new List<GitPullRequest>();

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var pullRequest = JsonSerializer.Deserialize<GitPullRequest>(reader.GetString(0));
            if (pullRequest != null)
            {
                pullRequests.Add(pullRequest);
            }
        }

        return pullRequests;
    }
    
    public async Task<GitPullRequest?> GetLatestCreatedPullRequest()
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT pr.* FROM main.PullRequests pr
                ORDER BY json_extract(pr.Data, '$.CreationDate') DESC
                LIMIT 1;
            ";
        
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var pullRequest = JsonSerializer.Deserialize<GitPullRequest>(reader.GetString(1));
            if (pullRequest != null)
            {
                return pullRequest;
            }
        }

        return null;
    }
    
    public async Task<GitPullRequest?> GetLatestClosedPullRequest()
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT pr.* FROM main.PullRequests pr
                ORDER BY json_extract(pr.Data, '$.ClosedDate') DESC
                LIMIT 1;
            ";
        
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var pullRequest = JsonSerializer.Deserialize<GitPullRequest>(reader.GetString(1));
            if (pullRequest != null)
            {
                return pullRequest;
            }
        }

        return null;
    }
}