using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Collections.Concurrent;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace TheMetz.Services
{
    public interface IPullRequestCommentService
    {
        public Task<IEnumerable<KeyValuePair<string, int>>> ShowCommentCounts(int numberOfDays);
        public List<(string Title, string Url)> GetDeveloperCommentLinks(string developerName);
    }

    internal class PullRequestCommentService : IPullRequestCommentService
    {
        private readonly VssConnection _connection;
        private ConcurrentDictionary<string, List<(string Title, string Url)>> _developerCommentLinks = new();
        private readonly IPullRequestService _pullRequestService;

        public PullRequestCommentService(VssConnection connection, IPullRequestService pullRequestService)
        {
            _connection = connection;
            _pullRequestService = pullRequestService;
        }

        public async Task<IEnumerable<KeyValuePair<string, int>>> ShowCommentCounts(int numberOfDays)
        {
            (List<string> CustomerOptimizationTeamMembers, List<string> OtherTeamMembers) allTeamMembers =
                GetAllTeamMembersAsync();

            List<GitPullRequest> pullRequests = await _pullRequestService.GetPullRequests(numberOfDays);

            var developerCommentCount = new ConcurrentDictionary<string, int>();
            _developerCommentLinks = new ConcurrentDictionary<string, List<(string, string)>>();

            List<GitPullRequest> filteredPullRequests = pullRequests
                .Where(pr =>
                    pr.SourceRefName != "refs/heads/develop"
                    && pr.SourceRefName != "refs/heads/Test"
                    && pr.CreatedBy.DisplayName != "Project Collection Build Service (DefaultCollection)"
                    && pr.Reviewers.Any(r =>
                        GetAllTeamMembersAsync().CustomerOptimizationTeamMembers.Contains(r.DisplayName))).ToList();

            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();

            foreach (GitPullRequest pr in filteredPullRequests)
            {
                try
                {
                    List<GitPullRequestCommentThread> threads = (await gitClient.GetThreadsAsync(
                        repositoryId: pr.Repository.Id,
                        pullRequestId: pr.PullRequestId
                    )).ToList();

                    IEnumerable<string> authorComments = threads.SelectMany(t => t.Comments)
                        .Where(c => c.CommentType != CommentType.System)
                        .GroupBy(c => c.Author.DisplayName)
                        .Select(c => c.Key).ToList();
                    foreach (string? authorComment in authorComments)
                    {
                        if (authorComment == pr.CreatedBy.DisplayName
                            || !allTeamMembers.CustomerOptimizationTeamMembers.Contains(authorComment))
                        {
                            continue;
                        }

                        if (_developerCommentLinks.TryGetValue(authorComment,
                                out List<(string Title, string Url)>? value)
                            && value.Any(r => r.Title == pr.Title))
                        {
                            continue;
                        }

                        developerCommentCount.AddOrUpdate(
                            authorComment,
                            1,
                            (_, count) => count + 1
                        );
                        _developerCommentLinks.AddOrUpdate(
                            authorComment,
                            _ => new List<(string, string)> { (pr.Title, GetFormattedPrUrl(pr)) },
                            (_, existingList) =>
                            {
                                existingList.Add((pr.Title, GetFormattedPrUrl(pr)));
                                return existingList;
                            }
                        );
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return developerCommentCount;
        }

        public List<(string Title, string Url)> GetDeveloperCommentLinks(string developerName)
        {
            return _developerCommentLinks[developerName].ToList();
        }

        private static string GetFormattedPrUrl(GitPullRequest pr)
        {
            return
                $"https://tfs.clarkinc.biz/DefaultCollection/{pr.Repository.ProjectReference.Name}/_git/{pr.Repository.Name}/pullrequest/{pr.PullRequestId}";
        }

        private static (List<string> CustomerOptimizationTeamMembers, List<string> OtherTeamMembers)
            GetAllTeamMembersAsync()
        {
            var customerOptimizationTeamMembers = new List<string>
            {
                "Dylan Manning",
                "David Acker",
                "Kerry Hannigan",
                "Liudmila Solovyeva",
                "Andrew Chanthavisith",
                "Michal Lesniewski",
                "Khrystyna Kregenbild"
            };
            var otherTeamMembers = new List<string>
            {
                "Phil Gathany",
                "Brandon George",
                "Shane Lieffers",
                "Shawn Dreier"
            };

            return (customerOptimizationTeamMembers, otherTeamMembers);
        }
    }
}