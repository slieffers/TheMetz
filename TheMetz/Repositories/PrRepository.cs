using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace TheMetz.Repositories;

public interface IPrRepository
{
    Task AddPullRequest(string prJson);
    Task UpdatePullRequest(int pullRequestId, string prJson);
    Task<List<GitPullRequest>> GetAllPullRequests();
    Task<List<GitPullRequest>> GetPullRequestsByDateOpened(DateTime dateOpened);
    Task<GitPullRequest> GetPullRequestByAdoPullRequestId(int pullRequestId);
    Task<List<GitPullRequest>> GetPullRequestsByDateClosed(DateTime dateClosed);
    Task<List<GitPullRequest>> GetPullRequestsByDateOpenedOrClosed(DateTime dateOpened, DateTime dateClosed);
    Task<GitPullRequest?> GetLatestCreatedPullRequest();
    Task<GitPullRequest?> GetOldestOpenPullRequest();
}

public class PrRepository : IPrRepository
{
    private readonly string _connectionString;

    public PrRepository()
    {
        // Specify the path to your SQLite database file
        _connectionString = "Data Source=../../../TheMetz.db"; 
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

    public async Task UpdatePullRequest(int pullRequestId, string prJson)
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                UPDATE main.PullRequests SET Data = $prJson
                WHERE PullRequestsId = $pullRequestId
            ";
        command.Parameters.AddWithValue("$prJson", prJson);
        command.Parameters.AddWithValue("$pullRequestId", pullRequestId);
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

    public async Task<List<GitPullRequest>> GetPullRequestsByDateOpened(DateTime dateOpened)
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT pr.Data FROM main.PullRequests pr
                WHERE DATETIME(json_extract(pr.Data, '$.CreationDate')) >= DATETIME($dateOpened)
            ";

        command.Parameters.AddWithValue("$dateOpened", dateOpened.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss"));
        
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
    
    public async Task<GitPullRequest?> GetPullRequestByAdoPullRequestId(int pullRequestId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT pr.Data FROM main.PullRequests pr
                WHERE json_extract(pr.Data, '$.PullRequestId') = $pullRequestId
            ";

        command.Parameters.AddWithValue("pullRequestId", pullRequestId);
        
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        
        GitPullRequest? pullRequestResult = null;
        while (await reader.ReadAsync())
        {
            if (pullRequestResult != null)
            {
                throw new ArgumentException("Multiple pull requests found with the same ADO Pull Request ID");
            }
            
            var pullRequest = JsonSerializer.Deserialize<GitPullRequest>(reader.GetString(0));
            if (pullRequest != null)
            {
                pullRequestResult = pullRequest;
            }
        }

        return pullRequestResult;
    }
    
    public async Task<List<GitPullRequest>> GetPullRequestsByDateClosed(DateTime dateClosed)
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();

        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
                SELECT pr.Data FROM main.PullRequests pr
                WHERE DATETIME(json_extract(pr.Data, '$.ClosedDate')) >= DATETIME($dateClosed)
            ";

        command.Parameters.AddWithValue("dateClosed", dateClosed.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss"));
        
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
    
    public async Task<List<GitPullRequest>> GetPullRequestsByDateOpenedOrClosed(DateTime dateOpened, DateTime dateClosed)
    {
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
        // command.CommandText = @"
        //         SELECT pr.* FROM main.PullRequests pr
        //         ORDER BY json_extract(pr.Data, '$.CreationDate') DESC
        //         LIMIT 1;
        //     ";
        
        command.CommandText = @"
                SELECT pr.* FROM main.PullRequests pr
                ORDER BY pr.DateUpdated DESC
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
    
    public async Task<GitPullRequest?> GetOldestOpenPullRequest()
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        await connection.OpenAsync();
        int status = (int)PullRequestStatus.Active;
        
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = @$"
                SELECT pr.* FROM main.PullRequests pr
                WHERE json_extract(pr.Data, '$.Status') = {status}
                ORDER BY json_extract(pr.Data, '$.CreationDate')
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