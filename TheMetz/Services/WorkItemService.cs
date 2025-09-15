using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using TheMetz.Interfaces;
using TheMetz.Models.DTO;
using TheMetz.Repositories;

namespace TheMetz.Services
{
    public interface IWorkItemService
    {
        Task<List<WorkItemDomainModel>> GetWorkItemsCompletedByDev(string devName, string workItemType = "Product Backlog Item");
        WorkItemDomainModel GetWorkItemsCompletedByDevLinks(string devName);
        Task<List<WorkItemDomainModel>> GetWorkItemsWithDeveloperReferenceSinceDate(DateTime sinceDate);
    }

    internal class WorkItemService : IWorkItemService
    {
        private readonly VssConnection _connection;
        private readonly ITeamMemberService _teamMemberService;
        private readonly IPullRequestService _pullRequestService;
        private readonly IWorkItemRepository _workItemRepository;
        private List<WorkItemDomainModel> _workItems = [];

        public WorkItemService(VssConnection connection, ITeamMemberService teamMemberService,
            IWorkItemRepository workItemRepository, IPullRequestService pullRequestService)
        {
            _connection = connection;
            _teamMemberService = teamMemberService;
            _workItemRepository = workItemRepository;
            _pullRequestService = pullRequestService;
        }

        public async Task<List<WorkItemDomainModel>> GetWorkItemsCompletedByDev(string devName,
            string workItemType = "Product Backlog Item")
        {
            var workItemTracking = await _connection.GetClientAsync<WorkItemTrackingHttpClient>();

            string query = @$"SELECT [System.Id], [System.Title], [System.AssignedTo]
                FROM workitems
                WHERE [System.AssignedTo] = '{devName}'
                AND [System.ChangedDate] >= '2025-08-11'";

            Wiql wiql = new() { Query = query };
            WorkItemQueryResult? result = await workItemTracking.QueryByWiqlAsync(wiql, top: 100);

            var workItemIds = result?.WorkItems?.Select(wi => wi.Id).Distinct().ToList();

            var results = new List<WorkItem>();
            if (workItemIds != null && workItemIds.Any())
            {
                var workItems = await workItemTracking.GetWorkItemsAsync(workItemIds, expand: WorkItemExpand.All);

                results = workItems;
            }

            _workItems = results
                .GroupBy(wi => ((IdentityRef)wi.Fields["System.AssignedTo"]).DisplayName).Select(t =>
                    new WorkItemDomainModel { DeveloperName = t.Key, WorkItems = t.DistinctBy(p => p.Id).ToList() })
                .ToList();

            return _workItems;
        }

        private Wiql GenerateWiqlByProject(string projectName,
            string developerDisplayName,
            string developerEmail,
            string startDate)
        {
            return new Wiql()
            {
                Query = $@"
            SELECT [System.Id], [System.Title], [System.AssignedTo]
            FROM WorkItems 
            WHERE [System.TeamProject] = '{projectName}' 
            AND [System.ChangedDate] >= '{startDate}'
            AND ([System.State] = 'Active' OR ([System.State] = 'Done' AND [Microsoft.VSTS.Common.ClosedDate] >= '{startDate}'))
            AND (
                [System.History] CONTAINS '{developerDisplayName}'
                OR [System.History] CONTAINS '{developerEmail}'
                OR [System.History] CONTAINS '@{developerDisplayName}'
                OR [System.ChangedBy] = '{developerDisplayName}'
                OR [System.AssignedTo] = '{developerDisplayName}'
                OR [System.CreatedBy] = '{developerDisplayName}'
            )
            ORDER BY [System.ChangedDate] DESC"
            };
        }

        public async Task<List<WorkItemDomainModel>> GetWorkItemsWithDeveloperReferenceSinceDate(DateTime sinceDate)
        {
            var workItemClient = await _connection.GetClientAsync<WorkItemTrackingHttpClient>();

            var startDate = sinceDate.ToString("yyyy-MM-dd");

            List<string> projects = new List<string>
                { "Marketplace", "Atlas", "Concorde", "Specialty Channel Development" };

            _workItems.Clear();

            List<TeamMember>? teamMembers = await _teamMemberService.GetCustomerOptimizationTeamMembers();

            foreach (TeamMember teamMember in teamMembers)
            {
                foreach (string project in projects)
                {
                    Wiql wiql = GenerateWiqlByProject(project, teamMember.Identity.DisplayName,
                        teamMember.Identity.UniqueName, startDate);

                    WorkItemQueryResult? result = await workItemClient.QueryByWiqlAsync(wiql);

                    if (result.WorkItems?.Any() != true)
                    {
                        continue;
                    }

                    int[] workItemIds = result.WorkItems.Select(wi => wi.Id).Distinct().ToArray();

                    var workItems = new List<WorkItem>();
                    
                    for (var i = 0; i < workItemIds.Length; i += 200)
                    {
                        workItems.AddRange(await workItemClient.GetWorkItemsAsync(
                            workItemIds.Skip(i).Take(200),
                            expand: WorkItemExpand.All));
                    }
                    
                    WorkItemDomainModel? entryToUpdate =
                        _workItems.FirstOrDefault(wi => wi.DeveloperName == teamMember.Identity.DisplayName);
                    if (entryToUpdate == null)
                    {
                        _workItems.Add(new WorkItemDomainModel
                            { DeveloperName = teamMember.Identity.DisplayName, WorkItems = workItems });
                    }
                    else
                    {
                        entryToUpdate.WorkItems.AddRange(workItems);
                    }
                }
            }

            return _workItems;
        }

        public WorkItemDomainModel GetWorkItemsCompletedByDevLinks(string devName)
        {
            return _workItems.First(wi => wi.DeveloperName == devName);
        }
    }
}