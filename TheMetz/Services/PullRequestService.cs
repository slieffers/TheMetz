using System.Text.Json;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Test.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using TheMetz.Repositories;

namespace TheMetz.Services
{
    public interface IPullRequestService
    {
        public Task<List<GitPullRequest>> GetPullRequestsByDateOpenedOrClosed(int numberOfDaysAgoOpened, int numberOfDaysAgoClosed = 0);
    }

    internal class PullRequestService : IPullRequestService
    {
        private readonly VssConnection _connection;
        private readonly IPrRepository _prRepository;
        private List<GitPullRequest> PullRequests = [];

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

        public PullRequestService(VssConnection connection, IPrRepository prRepository)
        {
            _connection = connection;
            _prRepository = prRepository;
        }

        public async Task<List<GitPullRequest>> GetPullRequestsByDateOpened(int numberOfDaysAgoOpened)
        {
            var prCutoffDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            DateTime openedFromDate = DateTime.Now.AddDays(-numberOfDaysAgoOpened);

            if (openedFromDate < prCutoffDate)
            {
                throw new ArgumentException(
                    "Cannot get pull requests before 2024-01-01 without manually changing the cutoff date.");
            }

            GitPullRequest? latestCreatedPr = await _prRepository.GetLatestCreatedPullRequest();
            GitPullRequest? latestClosedPr = await _prRepository.GetLatestClosedPullRequest();

            if (latestCreatedPr == null)
            {
                await StorePullRequestsFromDate(prCutoffDate, prCutoffDate);
            }
            else
            {
                DateTime latestResultCreationDateTime = latestCreatedPr.CreationDate;
                DateTime latestResultClosedDateTime = latestClosedPr!.ClosedDate;
            
                await StorePullRequestsFromDate(latestResultCreationDateTime, latestResultClosedDateTime);
            }

            List<GitPullRequest> filteredPrLoad = await _prRepository.GetPullRequestsByDateOpened(openedFromDate);
            PullRequests = filteredPrLoad.Where(p => p.CreationDate >= openedFromDate).ToList();

            return PullRequests;
        }

        public async Task<List<GitPullRequest>> GetPullRequestsByDateOpenedOrClosed(int numberOfDaysAgoOpened, int numberOfDaysAgoClosed = 0)
        {
            var prCutoffDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            DateTime openedFromDate = DateTime.Now.AddDays(-numberOfDaysAgoOpened);
            DateTime closedFromDate = numberOfDaysAgoClosed == 0 ? openedFromDate : DateTime.Now.AddDays(-numberOfDaysAgoClosed);
            if (openedFromDate < prCutoffDate || closedFromDate < prCutoffDate)
            {
                throw new ArgumentException(
                    "Cannot get pull requests before 2024-01-01 without manually changing the cutoff date.");
            }

            GitPullRequest? latestCreatedPr = await _prRepository.GetLatestCreatedPullRequest();
            GitPullRequest? latestClosedPr = await _prRepository.GetLatestClosedPullRequest();

            if (latestCreatedPr == null)
            {
                await StorePullRequestsFromDate(prCutoffDate, prCutoffDate);
            }
            else
            {
                DateTime latestResultCreationDateTime = latestCreatedPr.CreationDate;
                DateTime latestResultClosedDateTime = latestClosedPr!.ClosedDate;
            
                await StorePullRequestsFromDate(latestResultCreationDateTime, latestResultClosedDateTime);
            }

            List<GitPullRequest> filteredPrLoad = await _prRepository.GetPullRequestsByDateOpenedOrClosed(openedFromDate, closedFromDate);
            PullRequests = filteredPrLoad.Where(p => p.CreationDate >= openedFromDate || p.ClosedDate >= closedFromDate).ToList();

            return PullRequests;
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

        private async Task StorePullRequestsFromDate(DateTime prCreatedCutoffDate, DateTime prClosedCutoffDate)
        {
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
                        paginatedPullRequests = (await gitClient.GetPullRequestsAsync(
                            project: info.projectName,
                            repositoryId: repo.id,
                            searchCriteria: new GitPullRequestSearchCriteria
                                { Status = PullRequestStatus.All, IncludeLinks = true },
                            top: pageSize,
                            skip: skip
                        )).ToList();

                        if (paginatedPullRequests.Count != 0)
                        {
                            skip += pageSize;

                            List<string> createdPrs = paginatedPullRequests
                                .Where(pr =>
                                    pr.CreationDate > prCreatedCutoffDate)
                                .Select(pr => JsonSerializer.Serialize(pr)).ToList();

                            foreach (string prJsonString in createdPrs)
                            {
                                await _prRepository.AddPullRequest(prJsonString);
                            }
                            
                            IEnumerable<GitPullRequest> closedPrs = paginatedPullRequests
                                .Where(pr => pr.ClosedDate > prClosedCutoffDate);
                            foreach (GitPullRequest closedPr in closedPrs)
                            {
                                GitPullRequest pr = await _prRepository.GetPullRequestByAdoPullRequestId(closedPr.PullRequestId);
                                await _prRepository.UpdatePullRequest(pr.PullRequestId, JsonSerializer.Serialize(closedPr));
                            }    
                        }
                    } while (paginatedPullRequests.Count == pageSize &&
                             paginatedPullRequests.Any(pr =>
                                 pr.CreationDate > prCreatedCutoffDate
                                 || pr.ClosedDate > prClosedCutoffDate));
                }
            }
        }
    }
}