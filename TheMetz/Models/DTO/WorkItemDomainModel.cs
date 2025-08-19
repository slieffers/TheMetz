using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace TheMetz.Models.DTO;

public class WorkItemDomainModel
{
    public string DeveloperName { get; set; }
    public List<WorkItem> WorkItems { get; set; }

    // public string GetFormattedUrl(int id)
    // {
    //     return WorkItems.First(wi => wi.Id == id)
    // }
    //
    // private static string GetFormattedPrUrl(WorkItem wi)
    // {
    //     return
    //         $"https://tfs.clarkinc.biz/DefaultCollection/{pr.Repository.ProjectReference.Name}/_git/{pr.Repository.Name}/pullrequest/{pr.PullRequestId}";
    // }
}