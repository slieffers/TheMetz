using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace TheMetz.Interfaces;

public interface IPullRequestService
{
    public Task UpdateAdoPullRequests();
    public Task<List<GitPullRequest>> GetPullRequestsByDateOpened(int numberOfDaysAgoOpened);
    public Task<List<GitPullRequest>> GetPullRequestsByDateClosed(int numberOfDaysAgoClosed);
    public Task<List<GitPullRequest>> GetPullRequestsByDateOpenedOrClosed(int numberOfDaysAgoOpened, int numberOfDaysAgoClosed = 0);
}
