using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Concurrent;

namespace TheMetz.Services
{
    public interface IPullRequestStatsService
    {
        public Task<Dictionary<string, int>> ShowOpenedPrCounts(int numberOfDays);
        List<(string Title, string Url)> GetDeveloperOpenedPrLinks(string developerName);
    }

    internal class PullRequestStatsService : IPullRequestStatsService
    {
        private readonly VssConnection _connection;

        private Dictionary<string, List<(string Title, string Url)>> DeveloperCompletedPrLinks = new();
        private Dictionary<string, List<(string Title, string Url)>> DeveloperOpenedPrLinks = new();
        private Dictionary<string, List<(string Title, string Url)>> DeveloperReviewedPrLinks = new();

        private readonly IPullRequestService _pullRequestService;
        private readonly ITeamMemberService _teamMemberService;

        public PullRequestStatsService(VssConnection connection, IPullRequestService pullRequestService,
            ITeamMemberService teamMemberService)
        {
            _connection = connection;
            _pullRequestService = pullRequestService;
            _teamMemberService = teamMemberService;
        }

        public async Task<Dictionary<string, int>> ShowOpenedPrCounts(int numberOfDays)
        {
            List<GitPullRequest> pullRequests = await _pullRequestService.GetPullRequests(numberOfDays);

            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();

            DeveloperCompletedPrLinks.Clear();
            DeveloperOpenedPrLinks = new Dictionary<string, List<(string Title, string Url)>>();
            DeveloperReviewedPrLinks.Clear();

            List<TeamMember>? teamMembers = await _teamMemberService.GetCustomerOptimizationTeamMembers();
            if (teamMembers == null)
            {
                return new Dictionary<string, int>();
            }
            
            IEnumerable<GitPullRequest> openPrs = pullRequests
                .Where(pr => pr.CreationDate >= DateTime.Today.AddDays(-numberOfDays))
                .ToList();
            IEnumerable<GitPullRequest> customerOptimizationPullRequests = openPrs
                .Where(pr => teamMembers.Select(t => t.Identity.Id).Contains(pr.CreatedBy.Id))
                .ToList();

            Dictionary<string, int> teamMembersWithOpenPrs = customerOptimizationPullRequests.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Count());

            DeveloperOpenedPrLinks = openPrs.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Select(pr => (pr.Title, GetFormattedPrUrl(pr))).DistinctBy(p => p.Title).ToList());
            
            return teamMembersWithOpenPrs;
        }
        
        public List<(string Title, string Url)> GetDeveloperOpenedPrLinks(string developerName)
        {
            return DeveloperOpenedPrLinks[developerName].ToList();
        }

        private static string GetFormattedPrUrl(GitPullRequest pr)
        {
            return
                $"https://tfs.clarkinc.biz/DefaultCollection/{pr.Repository.ProjectReference.Name}/_git/{pr.Repository.Name}/pullrequest/{pr.PullRequestId}";
        }
    }
}