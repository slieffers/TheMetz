using System.Collections.ObjectModel;
using TheMetz.Interfaces;

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

        List<FSharp.Models.WorkItemInfo> workItems =
            (await WorkItemService.GetWorkItemsWithDeveloperReferenceSinceDate(DateTime.Today.Subtract(TimeSpan.FromDays(numberOfDaysToFetch)))).ToList();

        WorkItemResults.Clear();

        foreach (FSharp.Models.WorkItemInfo workItem in workItems)
        {
            WorkItemResults.Add(workItem.DeveloperName);
        }
    }
}