using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using TheMetz.Repositories;

namespace TheMetz.Services
{
    public interface IPullRequestService
    {
        public Task UpdateAdoPullRequests();
        public Task<List<GitPullRequest>> GetPullRequestsByDateOpened(int numberOfDaysAgoOpened);
        public Task<List<GitPullRequest>> GetPullRequestsByDateClosed(int numberOfDaysAgoClosed);
        public Task<List<GitPullRequest>> GetPullRequestsByDateOpenedOrClosed(int numberOfDaysAgoOpened, int numberOfDaysAgoClosed = 0);
    }

    internal class PullRequestService : IPullRequestService
    {
        private readonly VssConnection _connection;
        private readonly IPrRepository _prRepository;

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
        private readonly DateTime _prCutoffDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public PullRequestService(VssConnection connection, IPrRepository prRepository)
        {
            _connection = connection;
            _prRepository = prRepository;
        }

        public async Task<List<GitPullRequest>> GetPullRequestsByDateOpened(int numberOfDaysAgoOpened)
        {
            DateTime openedFromDate = DateTime.Now.AddDays(-numberOfDaysAgoOpened);
            if (openedFromDate < _prCutoffDate)
            {
                throw new ArgumentException(
                    "Cannot get pull requests before 2024-01-01 without manually changing the cutoff date.");
            }
            
            return await _prRepository.GetPullRequestsByDateOpened(openedFromDate);
        }

        public async Task<List<GitPullRequest>> GetPullRequestsByDateClosed(int numberOfDaysAgoClosed)
        {
            DateTime closedFromDate = DateTime.Now.AddDays(-numberOfDaysAgoClosed);
            if (closedFromDate < _prCutoffDate)
            {
                throw new ArgumentException(
                    "Cannot get pull requests before 2024-01-01 without manually changing the cutoff date.");
            }
            
            return await _prRepository.GetPullRequestsByDateClosed(closedFromDate);
        }

        public async Task<List<GitPullRequest>> GetPullRequestsByDateOpenedOrClosed(int numberOfDaysAgoOpened, int numberOfDaysAgoClosed = 0)
        {
            DateTime openedFromDate = DateTime.Now.AddDays(-numberOfDaysAgoOpened);
            DateTime closedFromDate = numberOfDaysAgoClosed == 0 ? openedFromDate : DateTime.Now.AddDays(-numberOfDaysAgoClosed);
            if (openedFromDate < _prCutoffDate || closedFromDate < _prCutoffDate)
            {
                throw new ArgumentException(
                    "Cannot get pull requests before 2024-01-01 without manually changing the cutoff date.");
            }


            return await _prRepository.GetPullRequestsByDateOpenedOrClosed(openedFromDate, closedFromDate);
        }

        public async Task UpdateAdoPullRequests()
        {
            (DateTime dateUpdated, GitPullRequest? gitPullRequest) latestCreatedPr = await _prRepository.GetLatestCreatedPullRequest();
            GitPullRequest? oldestOpenPr = await _prRepository.GetOldestOpenPullRequest();

            if (latestCreatedPr.gitPullRequest == null)
            {
                await StorePullRequestsFromDate(_prCutoffDate, _prCutoffDate);
            }
            else
            {
                DateTime latestResultCreationDateTime = latestCreatedPr.dateUpdated;
                DateTime oldestResultOpenDateTime = oldestOpenPr!.CreationDate;
            
                await StorePullRequestsFromDate(latestResultCreationDateTime, oldestResultOpenDateTime);
            }
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

        private async Task GetAndUpdatePullRequest(GitPullRequest pullRequest, GitHttpClient gitClient)
        {
            GitPullRequest? adoPr = await gitClient.GetPullRequestByIdAsync(pullRequest.PullRequestId);
            if (adoPr != null)
            {
                await _prRepository.UpdatePullRequestByADOPullRequestId(adoPr.PullRequestId, JsonSerializer.Serialize(adoPr));
            }
        }
        
        private async Task StorePullRequestsFromDate(DateTime prCreatedCutoffDate, DateTime prOpenCutoffDate)
        {
            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();
        
            List<GitPullRequest> openPullRequests = await _prRepository.GetOpenPullRequests();
            var openPullRequestsTaskList = new List<Task>();
            foreach (GitPullRequest openPullRequest in openPullRequests)
            {
                openPullRequestsTaskList.Add(GetAndUpdatePullRequest(openPullRequest, gitClient));
            }

            await Task.WhenAll(openPullRequestsTaskList);
            
            IEnumerable<string> projectNames = _projectInfo.Select(p => p.projectName).ToList();
            foreach (string projectName in projectNames)
            {
                await LoadProjectRepos(gitClient, projectName);
            }
        
            foreach ((string projectName, List<(string name, Guid id)> repos) info in _projectInfo)
            {
                {
                    var skip = 0;
                    const int pageSize = 100;
                    List<GitPullRequest> paginatedPullRequests;
        
                    do
                    {
                        paginatedPullRequests = (await gitClient.GetPullRequestsByProjectAsync(
                            project: info.projectName,
                            searchCriteria: new GitPullRequestSearchCriteria
                                { Status = PullRequestStatus.All, IncludeLinks = true, MinTime = prOpenCutoffDate},
                            top: pageSize,
                            skip: skip
                        )).ToList();
        
                        if (paginatedPullRequests.Count != 0)
                        {
                            skip += pageSize;
        
                            await ExtractAndStoreCreatedPullRequests(prCreatedCutoffDate, paginatedPullRequests.Except(openPullRequests).ToList());
        
                        }
                    } while (paginatedPullRequests.Count == pageSize);
                }
            }
        }

        private async Task ExtractAndStoreCreatedPullRequests(DateTime prCreatedCutoffDate, List<GitPullRequest> paginatedPullRequests)
        {
            List<string> createdPrs = paginatedPullRequests
                .Where(pr =>
                    pr.CreationDate > prCreatedCutoffDate)
                .Select(pr => JsonSerializer.Serialize(pr)).ToList();

            foreach (string prJsonString in createdPrs)
            {
                await _prRepository.AddPullRequest(prJsonString);
            }
        }
    }
}