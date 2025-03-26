using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.Services.Common;

namespace TheMetz.Services
{
    public interface IPullRequestEffortService
    {
        public Task<Dictionary<string, int>> ShowDeveloperEffortCounts(int numberOfDays);
        // public Task<Dictionary<string, int>> ShowAverageEffortPerPrCounts(int numberOfDays);
        // List<(string Title, string Url)> GetDeveloperEffortCounts(string developerName);
        // List<(string Title, string Url)> GetAverageEffortPerPrCounts(string developerName);
    }

    internal class PullRequestEffortService : IPullRequestEffortService
    {
        private readonly VssConnection _connection;

        private Dictionary<string, List<(string Title, string Url)>> _developerEffortPointLinks = new();
        private Dictionary<string, List<(string Title, string Url)>> _developerOpenedPrLinks = new();
        private Dictionary<string, List<(string Title, string Url)>> _developerReviewedPrLinks = new();

        private readonly IPullRequestService _pullRequestService;
        private readonly ITeamMemberService _teamMemberService;

        public PullRequestEffortService(VssConnection connection, IPullRequestService pullRequestService,
            ITeamMemberService teamMemberService)
        {
            _connection = connection;
            _pullRequestService = pullRequestService;
            _teamMemberService = teamMemberService;
        }

        public async Task<Dictionary<string, int>> ShowDeveloperEffortCounts(int numberOfDays)
        {
            List<GitPullRequest> pullRequests = await _pullRequestService.GetPullRequestsByDateOpenedOrClosed(numberOfDays);

            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();

            _developerEffortPointLinks.Clear();

            List<TeamMember>? teamMembers = await _teamMemberService.GetCustomerOptimizationTeamMembers();
            if (teamMembers == null)
            {
                return new Dictionary<string, int>();
            }
            
            IEnumerable<GitPullRequest> openedPrs = pullRequests
                .Where(pr => pr.CreationDate >= DateTime.Today.AddDays(-numberOfDays))
                .ToList();
            IEnumerable<GitPullRequest> customerOptimizationPullRequests = openedPrs
                .Where(pr => teamMembers.Select(t => t.Identity.Id).Contains(pr.CreatedBy.Id))
                .ToList();

            Dictionary<string, int> teamMembersEffortLevelStats = customerOptimizationPullRequests.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Count());

            _developerOpenedPrLinks = openedPrs.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Select(pr => (pr.Title, GetFormattedPrUrl(pr))).DistinctBy(p => p.Title).ToList());
            
            return teamMembersEffortLevelStats;
        }
        //
        // public async Task<Dictionary<string, int>> ShowClosedPrCounts(int numberOfDays)
        // {
        //     List<GitPullRequest> pullRequests = await _pullRequestService.GetPullRequests(numberOfDays);
        //
        //     using var gitClient = await _connection.GetClientAsync<GitHttpClient>();
        //
        //     _developerClosedPrLinks.Clear();
        //
        //     List<TeamMember>? teamMembers = await _teamMemberService.GetCustomerOptimizationTeamMembers();
        //     if (teamMembers == null)
        //     {
        //         return new Dictionary<string, int>();
        //     }
        //     
        //     IEnumerable<GitPullRequest> closedPrs = pullRequests
        //         .Where(pr => pr.ClosedDate >= DateTime.Today.AddDays(-numberOfDays))
        //         .ToList();
        //     IEnumerable<GitPullRequest> customerOptimizationPullRequests = closedPrs
        //         .Where(pr => teamMembers.Select(t => t.Identity.Id).Contains(pr.CreatedBy.Id))
        //         .ToList();
        //
        //     Dictionary<string, int> teamMembersClosedPrStats = customerOptimizationPullRequests.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Count());
        //
        //     _developerClosedPrLinks = closedPrs.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Select(pr => (pr.Title, GetFormattedPrUrl(pr))).DistinctBy(p => p.Title).ToList());
        //     
        //     return teamMembersClosedPrStats;
        // }
        //
        // public List<(string Title, string Url)> GetDeveloperOpenedPrLinks(string developerName)
        // {
        //     return _developerOpenedPrLinks[developerName].ToList();
        // }
        //
        // public List<(string Title, string Url)> GetDeveloperClosedPrLinks(string developerName)
        // {
        //     return _developerClosedPrLinks[developerName].ToList();
        // }
        //
        private static string GetFormattedPrUrl(GitPullRequest pr)
        {
            return
                $"https://tfs.clarkinc.biz/DefaultCollection/{pr.Repository.ProjectReference.Name}/_git/{pr.Repository.Name}/pullrequest/{pr.PullRequestId}";
        }
    }
}