using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using TheMetz.Models.DTO;
using TheMetz.Services;

namespace TheMetz.Models;

public class WorkItemStatsViewModel
{
    public readonly IWorkItemService WorkItemService;

    public ObservableCollection<string> WorkItemResults { get; } = new();

    public WorkItemStatsViewModel(IWorkItemService workItemService)
    {
        WorkItemService = workItemService;
    }

    public async Task LoadWorkItemData(int numberOfDaysToFetch)
    {
        WorkItemResults.Clear();

        WorkItemResults.Add("Loading...");

        List<WorkItemDomainModel> workItems =
            await WorkItemService.GetWorkItemsWithDeveloperReferenceSinceDate(DateTime.Today.Subtract(TimeSpan.FromDays(numberOfDaysToFetch)));

        WorkItemResults.Clear();

        foreach (WorkItemDomainModel workItem in workItems)
        {
            WorkItemResults.Add(workItem.DeveloperName);
        }
    }
}