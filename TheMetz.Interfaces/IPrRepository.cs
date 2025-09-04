using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace TheMetz.Interfaces;

public interface IPrRepository
{
    Task AddPullRequest(string prJson);
    Task UpdatePullRequest(int pullRequestId, string prJson);
    Task<List<GitPullRequest>> GetAllPullRequests();
    Task<List<GitPullRequest>> GetPullRequestsByDateOpened(DateTime dateOpened);
    Task<GitPullRequest> GetPullRequestByAdoPullRequestId(int pullRequestId);
    Task<List<GitPullRequest>> GetPullRequestsByDateClosed(DateTime dateClosed);
    Task<List<GitPullRequest>> GetPullRequestsByDateOpenedOrClosed(DateTime dateOpened, DateTime dateClosed);
    Task<(DateTime dateUpdated, GitPullRequest?)> GetLatestCreatedPullRequest();
    Task<GitPullRequest?> GetOldestOpenPullRequest();
    Task<List<GitPullRequest>> GetOpenPullRequests();
    Task UpdatePullRequestByADOPullRequestId(int adoPullRequestId, string prJson);

}