using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace TheMetz.Services
{
    public interface IPullRequestService
    {
        public Task<List<GitPullRequest>> GetPullRequests(int numberOfDays);
    }

    internal class PullRequestService : IPullRequestService
    {
        private readonly VssConnection _connection;
        private readonly List<GitPullRequest> _pullRequests = [];

        private readonly List<(string projectName, List<(string, Guid)> repos)> _projectInfo =
        [
            ("Marketplace", new List<(string repoName, Guid repoId)>
            {
                ("Marketplace", Guid.Empty),
                ("MarketplaceIDSConsumers", Guid.Empty),
                ("Clark Chains", Guid.Empty),
                ("Marketplace Customer Configuration Consumers", Guid.Empty),
                ("ClarkChainsDatabase", Guid.Empty),
                ("Marketplace-react-app", Guid.Empty),
                ("MarketplaceFrontends", Guid.Empty)
            }),
            ("Concorde", new List<(string repoName, Guid repoId)>
            {
                ("Concorde", Guid.Empty),
                ("Clark.Concorde.Models", Guid.Empty),
                ("ConcordeConsumers", Guid.Empty),
                ("ConcordeDatabase", Guid.Empty)
            }),
            ("Atlas", new List<(string repoName, Guid repoId)>
            {
                ("Atlas", Guid.Empty),
                ("Atlas.SalesHistoryModal", Guid.Empty),
                ("AtlasConsumers", Guid.Empty),
                ("AtlasDatabase", Guid.Empty)
            }),
            ("Specialty Channel Development", new List<(string repoName, Guid repoId)>
            {
                ("SpecChannelAirport", Guid.Empty),
                ("SpecChannelAirportDatabase", Guid.Empty),
                ("SpecChannelAirportConsumers", Guid.Empty)
            })
        ];

        private int _currentNumberOfDays = 0;

        public PullRequestService(VssConnection connection)
        {
            _connection = connection;
        }

        public async Task<List<GitPullRequest>> GetPullRequests(int numberOfDays)
        {
            DateTime fromDate = DateTime.Now.AddDays(-numberOfDays);

            if (_pullRequests.Count != 0 && _currentNumberOfDays >= numberOfDays)
            {
                return _pullRequests.Where(p => p.CreationDate >= fromDate || p.ClosedDate >= fromDate).ToList();
            }

            _currentNumberOfDays = numberOfDays;

            _pullRequests.Clear();

            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();

            IEnumerable<string> projectNames = _projectInfo.Select(p => p.projectName).ToList();
            foreach (string projectName in projectNames)
            {
                await LoadProjectRepos(gitClient, projectName);
            }

            foreach ((string projectName, List<(string name, Guid id)> repos) info in _projectInfo)
            {
                foreach ((string name, Guid id) repo in info.repos)
                {
                    var skip = 0;
                    const int pageSize = 100;
                    List<GitPullRequest> paginatedPullRequests;

                    do
                    {
                        paginatedPullRequests = await gitClient.GetPullRequestsAsync(
                            project: info.projectName,
                            repositoryId: repo.id,
                            searchCriteria: new GitPullRequestSearchCriteria
                                { Status = PullRequestStatus.All },
                            top: pageSize,
                            skip: skip
                        );

                        if (paginatedPullRequests.Any())
                        {
                            _pullRequests.AddRange(paginatedPullRequests);
                            skip += pageSize;
                        }
                    } while (paginatedPullRequests.Count == pageSize &&
                             paginatedPullRequests.Any(pr => 
                                 pr.CreationDate >= fromDate 
                                 || pr.ClosedDate >= fromDate));
                }
            }

            return _pullRequests.Where(p => p.CreationDate >= fromDate || p.ClosedDate >= fromDate).ToList();
        }

        private async Task LoadProjectRepos(GitHttpClient gitClient, string projectName)
        {
            (string projectName, List<(string, Guid)> repos) repoInfo =
                _projectInfo.First(p => p.projectName == projectName);

            IEnumerable<(string Name, Guid Id)> repos = (await gitClient.GetRepositoriesAsync(repoInfo.projectName))
                    .Select(r => (r.Name, r.Id))
                    .Where(r => repoInfo.repos.Any(ri =>
                    {
                        (string repoName, Guid _) = ri;
                        return repoName == r.Name;
                    })).ToList()
                ;

            repoInfo.repos = repos.ToList();
            (string projectName, List<(string, Guid)> repos) project =
                _projectInfo.First(p => p.projectName == projectName);
            _projectInfo[_projectInfo.IndexOf(project)] = repoInfo;
        }
    }
}