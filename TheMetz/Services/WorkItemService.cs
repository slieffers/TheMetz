using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using TheMetz.Repositories;

namespace TheMetz.Services
{
    public interface IWorkItemService
    {
        public Task<Dictionary<string, List<WorkItem>>> GetWorkItems(List<int> workItemIds);
    }

    internal class WorkItemService : IWorkItemService
    {
        private readonly VssConnection _connection;
        private readonly ITeamMemberService _teamMemberService;
        private readonly IPullRequestService _pullRequestService;
        private readonly IWorkItemRepository _workItemRepository;
        private readonly Dictionary<string, List<WorkItem>> _workItems = [];

        public WorkItemService(VssConnection connection, ITeamMemberService teamMemberService,
            IWorkItemRepository workItemRepository, IPullRequestService pullRequestService)
        {
            _connection = connection;
            _teamMemberService = teamMemberService;
            _workItemRepository = workItemRepository;
            _pullRequestService = pullRequestService;
        }

        public async Task<Dictionary<string, List<WorkItem>>> GetWorkItems(List<int> workItemIds)
        {
            var teamMembers = await _teamMemberService.GetCustomerOptimizationTeamMembers();
            foreach (TeamMember teamMember in teamMembers)
            {
                _workItems.Add(teamMember.Identity.DisplayName,
                    await GetWorkItemsCompletedByDev(teamMember.Identity.DisplayName));
            }

            var results = new Dictionary<string, int>();
            foreach (var dev in _workItems)
            {
                var effortFieldPBIs = dev.Value.Where(x =>
                    x.Fields != null && x.Fields.ContainsKey("Microsoft.VSTS.Scheduling.Effort"));

                var avarageEffort = effortFieldPBIs.Average(x => (double)x.Fields["Microsoft.VSTS.Scheduling.Effort"]);
            }
            // if (_pullRequests.Count != 0)
            // {
            //     return _pullRequests.Where(p => p.CreationDate >= fromDate || p.ClosedDate >= fromDate).ToList();
            // }
            //
            // _workItems.Clear();
            //
            // using var gitClient = await _connection.GetClientAsync<WorkItemTrackingHttpClient>();
            // gitClient.GetWorkItemsAsync(workItemIds);
            //
            // IEnumerable<string> projectNames = _projectInfo.Select(p => p.projectName).ToList();
            // foreach (string projectName in projectNames)
            // {
            //     await LoadProjectRepos(gitClient, projectName);
            // }
            //
            // foreach ((string projectName, List<(string name, Guid id)> repos) info in _projectInfo)
            // {
            //     foreach ((string name, Guid id) repo in info.repos)
            //     {
            //         var skip = 0;
            //         const int pageSize = 100;
            //         List<GitPullRequest> paginatedPullRequests;
            //
            //         do
            //         {
            //             paginatedPullRequests = await gitClient.GetPullRequestsAsync(
            //                 project: info.projectName,
            //                 repositoryId: repo.id,
            //                 searchCriteria: new GitPullRequestSearchCriteria
            //                     { Status = PullRequestStatus.All, IncludeLinks = true},
            //                 top: pageSize,
            //                 skip: skip
            //             );
            //
            //             if (paginatedPullRequests.Any())
            //             {
            //                 _pullRequests.AddRange(paginatedPullRequests);
            //                 skip += pageSize;
            //             }
            //         } while (paginatedPullRequests.Count == pageSize &&
            //                  paginatedPullRequests.Any(pr => 
            //                      pr.CreationDate >= fromDate 
            //                      || pr.ClosedDate >= fromDate));
            //     }
            // }
            //
            // return _pullRequests.Where(p => p.CreationDate >= fromDate || p.ClosedDate >= fromDate).ToList();

            return _workItems;
        }

        private async Task<List<WorkItem>> GetWorkItemsCompletedByDev(string devName,
            string workItemType = "Product Backlog Item")
        {
            var workItemTracking = await _connection.GetClientAsync<WorkItemTrackingHttpClient>();

            // string query = @$"SELECT [System.Id], [System.Title], [System.AssignedTo]
            //     FROM workitems
            //     WHERE [System.AssignedTo] = '{devName}'
            //     AND [System.State] = 'Done'
            //     AND [System.ChangedDate] >= '2024-01-01'
            //     AND [System.WorkItemType] = '{workItemType}'
            //     ORDER BY [System.ChangedDate] DESC";
            //
            string query = @$"SELECT [System.Id], [System.Title], [System.AssignedTo]
                FROM workitems
                WHERE [System.AssignedTo] = '{devName}'
                AND [System.ChangedDate] >= '2024-01-01'";

            Wiql wiql = new() { Query = query };
            var result = await workItemTracking.QueryByWiqlAsync(wiql, top: 100);

            var workItemIds = result?.WorkItems?.Select(wi => wi.Id).ToList();

            var results = new List<WorkItem>();
            if (workItemIds != null && workItemIds.Any())
            {
                var workItems = await workItemTracking.GetWorkItemsAsync(workItemIds, expand: WorkItemExpand.All);

                results = workItems;
            }

            return results.ToList();
        }
    }
}