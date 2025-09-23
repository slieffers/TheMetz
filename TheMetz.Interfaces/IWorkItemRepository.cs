using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace TheMetz.Interfaces;

public interface IWorkItemRepository
{
    Task AddWorkItem(string workItemJson);
    Task<List<WorkItem>> GetAllWorkItems();
    Task<List<WorkItem>> GetWorkItemsByDateCreated(DateTime dateCreated);
    Task<WorkItem?> GetLatestCreatedWorkItem();
    Task<WorkItem?> GetLatestClosedWorkItem();
}