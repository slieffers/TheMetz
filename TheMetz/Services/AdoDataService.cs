using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Concurrent;

namespace TheMetz.Services
{
    internal class AdoDataService
    {
        private readonly VssConnection _connection;
        private readonly List<GitPullRequest> _pullRequests = [];
        private readonly List<string> _projectNames = ["Marketplace", "Concorde", "Atlas", "Specialty Channel Development"];
        private static readonly ConcurrentDictionary<string, List<(string Title, string Url)>> DeveloperCommentLinks = new();

        public AdoDataService(VssConnection connection)
        {
            _connection = connection;
        }

        private async Task LoadPullRequests()
        {
            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();

            DateTime fromDate = DateTime.UtcNow.AddDays(-14);

            foreach (string projectName in _projectNames)
            {
                _pullRequests.AddRange(await gitClient.GetPullRequestsByProjectAsync(
                    project: projectName,
                    searchCriteria: new GitPullRequestSearchCriteria { MinTime = fromDate  }
                ));
            }
        }
        
        public async Task<IEnumerable<KeyValuePair<string, int>>> ShowCommentCounts()
        {
            (List<string> CustomerOptimizationTeamMembers, List<string> OtherTeamMembers) allTeamMembers = GetAllTeamMembersAsync();
            
            await LoadPullRequests();
            
            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();

            var developerCommentCount = new ConcurrentDictionary<string, int>();

            await Task.WhenAll(_pullRequests.Select(async pr =>
            {
                List<GitPullRequestCommentThread>? threads = await gitClient.GetThreadsAsync(
                    repositoryId: pr.Repository.Id,
                    pullRequestId: pr.PullRequestId
                );
                
                IEnumerable<string> authorDisplayNames = threads.SelectMany(t => t.Comments)
                    .Where(c => c.CommentType != CommentType.System)
                    .GroupBy(c => c.Author.DisplayName)
                    .Select(c => c.Key);
                foreach (string? authorDisplayName in authorDisplayNames)
                {
                    if (authorDisplayName == pr.CreatedBy.DisplayName
                        || !allTeamMembers.CustomerOptimizationTeamMembers.Contains(authorDisplayName))
                    {
                        continue;
                    }
                    
                    if (DeveloperCommentLinks.TryGetValue(authorDisplayName, out List<(string Title, string Url)>? value) 
                        && value.Any(r => r.Title == pr.Title))
                    {
                        continue;
                    }
                        
                    developerCommentCount.AddOrUpdate(
                        authorDisplayName,
                        1,
                        (_, count) => count + 1
                    );
                    DeveloperCommentLinks.AddOrUpdate(
                        authorDisplayName,
                        _ => new List<(string, string)> { (pr.Title, GetFormattedPrUrl(pr)) },
                        (_, existingList) => { existingList.Add((pr.Title, GetFormattedPrUrl(pr))); return existingList; }
                    );
                }
            }));

            return developerCommentCount;
        }

        private static string GetFormattedPrUrl(GitPullRequest pr)
        {
            return $"https://tfs.clarkinc.biz/DefaultCollection/{pr.Repository.ProjectReference.Name}/_git/{pr.Repository.Name}/pullrequest/{pr.PullRequestId}";
        }

        private static (List<string> CustomerOptimizationTeamMembers, List<string> OtherTeamMembers) GetAllTeamMembersAsync()
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

        public List<(string Title, string Url)> GetDeveloperCommentLinks(string developerName)
        {
            return DeveloperCommentLinks[developerName].ToList();
        }
    }
}