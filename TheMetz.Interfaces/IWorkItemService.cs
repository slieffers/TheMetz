namespace TheMetz.Interfaces;

public interface IWorkItemService
{
    FSharp.Models.WorkItemInfo GetWorkItemsCompletedByDevLinks(string devName);
    Task<IEnumerable<FSharp.Models.WorkItemInfo>> GetWorkItemsWithDeveloperReferenceSinceDate(DateTime sinceDate);
}