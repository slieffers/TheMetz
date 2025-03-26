using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Concurrent;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace TheMetz.Services
{
    public interface IPullRequestStateChangeService
    {
        public Task<Dictionary<string, int>> ShowOpenedPrCounts(int numberOfDays);
        public Task<Dictionary<string, int>> ShowClosedPrCounts(int numberOfDays);
        public Task<Dictionary<string, int>> ShowReviewedPrCounts(int numberOfDays);
        List<(string Title, string Url)> GetDeveloperOpenedPrLinks(string developerName);
        List<(string Title, string Url)> GetDeveloperClosedPrLinks(string developerName);
        List<(string Title, string Url)> GetDeveloperReviewedPrLinks(string developerName);
    }

    internal class PullRequestStateChangeService : IPullRequestStateChangeService
    {
        private readonly VssConnection _connection;

        private Dictionary<string, List<(string Title, string Url)>> _developerClosedPrLinks = new();
        private Dictionary<string, List<(string Title, string Url)>> _developerOpenedPrLinks = new();
        private Dictionary<string, List<(string Title, string Url)>> _developerReviewedPrLinks = new();

        private readonly IPullRequestService _pullRequestService;
        private readonly ITeamMemberService _teamMemberService;

        public PullRequestStateChangeService(VssConnection connection, IPullRequestService pullRequestService,
            ITeamMemberService teamMemberService)
        {
            _connection = connection;
            _pullRequestService = pullRequestService;
            _teamMemberService = teamMemberService;
        }

        public async Task<Dictionary<string, int>> ShowOpenedPrCounts(int numberOfDays)
        {
            List<GitPullRequest> pullRequests = await _pullRequestService.GetPullRequestsByDateOpenedOrClosed(numberOfDays);

            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();

            _developerOpenedPrLinks.Clear();

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
            // var test = (ReferenceLink)customerOptimizationPullRequests.First().Links.Links["workItems"];
            // var test2 = await gitClient.GetPullRequestWorkItemRefsAsync("Marketplace", new Guid("c16d6b18-39a8-4755-b59c-528bd8b950a1"), 338388);
            // var workItemClient = await _connection.GetClientAsync<WorkItemTrackingHttpClient>();
            // var workItems = await workItemClient.GetWorkItemsAsync([1290620]);

            
            Dictionary<string, int> teamMembersOpenPrStats = customerOptimizationPullRequests.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Count());

            _developerOpenedPrLinks = openPrs.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Select(pr => (pr.Title, GetFormattedPrUrl(pr))).DistinctBy(p => p.Title).ToList());
            
            return teamMembersOpenPrStats;
        }
        
        public async Task<Dictionary<string, int>> ShowClosedPrCounts(int numberOfDays)
        {
            List<GitPullRequest> pullRequests = await _pullRequestService.GetPullRequestsByDateOpenedOrClosed(numberOfDays);

            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();

            _developerClosedPrLinks.Clear();

            List<TeamMember>? teamMembers = await _teamMemberService.GetCustomerOptimizationTeamMembers();
            if (teamMembers == null)
            {
                return new Dictionary<string, int>();
            }
            
            IEnumerable<GitPullRequest> closedPrs = pullRequests
                .Where(pr => pr.ClosedDate >= DateTime.Today.AddDays(-numberOfDays))
                .ToList();
            IEnumerable<GitPullRequest> customerOptimizationPullRequests = closedPrs
                .Where(pr => teamMembers.Select(t => t.Identity.Id).Contains(pr.CreatedBy.Id))
                .ToList();

            Dictionary<string, int> teamMembersClosedPrStats = customerOptimizationPullRequests.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Count());

            _developerClosedPrLinks = closedPrs.GroupBy(pr => pr.CreatedBy.DisplayName).ToDictionary(t => t.Key, t => t.Select(pr => (pr.Title, GetFormattedPrUrl(pr))).DistinctBy(p => p.Title).ToList());
            
            return teamMembersClosedPrStats;
        }
        
        public async Task<Dictionary<string, int>> ShowReviewedPrCounts(int numberOfDays)
        {
            List<GitPullRequest> pullRequests = await _pullRequestService.GetPullRequestsByDateOpenedOrClosed(numberOfDays);
            pullRequests = pullRequests.Where(pr => pr.CreationDate >= DateTime.Today.AddDays(-numberOfDays)).ToList();
            
            _developerReviewedPrLinks.Clear();

            List<TeamMember>? teamMembers = await _teamMemberService.GetCustomerOptimizationTeamMembers();
            if (teamMembers == null)
            {
                return new Dictionary<string, int>();
            }
            List<string> teamMemberNames = teamMembers.Select(t => t.Identity.DisplayName).ToList();
            IEnumerable<GitPullRequest> reviewedPrs = pullRequests
                .Where(pr => 
                    (pr.Status == PullRequestStatus.Active || pr.ClosedDate >= DateTime.Today.AddDays(-numberOfDays))
                     && pr.Reviewers.ToList().Exists(r => teamMemberNames.Contains(r.DisplayName))).ToList();

            var teamMemberReviewerStats = new Dictionary<string, int>();
            foreach (GitPullRequest reviewedPr in reviewedPrs)
            {
                foreach (IdentityRefWithVote reviewer in reviewedPr.Reviewers)
                {
                    if (!teamMemberNames.Contains(reviewer.DisplayName) || reviewer.Vote == 0)
                    {
                        continue;
                    }
                    
                    if(!teamMemberReviewerStats.TryAdd(reviewer.DisplayName, 1))
                    {
                        teamMemberReviewerStats[reviewer.DisplayName] += 1;
                    }

                    if (!_developerReviewedPrLinks.TryGetValue(reviewer.DisplayName, out List<(string Title, string Url)>? value))
                    {
                        _developerReviewedPrLinks.Add(reviewer.DisplayName, new List<(string, string)> { (reviewedPr.Title, GetFormattedPrUrl(reviewedPr)) });
                    }
                    else
                    {
                        value.Add((reviewedPr.Title, GetFormattedPrUrl(reviewedPr)));
                    }
                }
            }
            
            return teamMemberReviewerStats;
        }
        
        public List<(string Title, string Url)> GetDeveloperOpenedPrLinks(string developerName)
        {
            return _developerOpenedPrLinks[developerName].ToList();
        }
        
        public List<(string Title, string Url)> GetDeveloperClosedPrLinks(string developerName)
        {
            return _developerClosedPrLinks[developerName].ToList();
        }

        public List<(string Title, string Url)> GetDeveloperReviewedPrLinks(string developerName)
        {
            return _developerReviewedPrLinks[developerName].ToList();
        }

        private static string GetFormattedPrUrl(GitPullRequest pr)
        {
            return
                $"https://tfs.clarkinc.biz/DefaultCollection/{pr.Repository.ProjectReference.Name}/_git/{pr.Repository.Name}/pullrequest/{pr.PullRequestId}";
        }
    }
}