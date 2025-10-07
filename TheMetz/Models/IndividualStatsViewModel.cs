using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using TheMetz.Interfaces;
using TheMetz.Services;

namespace TheMetz.Models;

public class IndividualStatsViewModel
{
    public readonly IPullRequestCommentService PullRequestCommentService;
    public readonly IWorkItemService WorkItemService;
    public readonly IPullRequestStateChangeService PullRequestStateChangeService;
    private readonly ITeamMemberService _teamMemberService;

    public ObservableCollection<string> TeamMembers { get; } = new();

    private Dictionary<string, FSharp.Models.ReviewCounts> _commentData = new();
    private Dictionary<string, List<FSharp.Models.Link>> _commentLinks = new();
    private Dictionary<string, List<WorkItem>> _workItemData = new();
    private Dictionary<string, int> _prOpenedData = new();
    private Dictionary<string, int> _prClosedData = new();
    private Dictionary<string, int> _prReviewedData = new();

    public IndividualStatsViewModel(
        IPullRequestCommentService pullRequestCommentService,
        IWorkItemService workItemService,
        IPullRequestStateChangeService pullRequestStateChangeService,
        ITeamMemberService teamMemberService)
    {
        PullRequestCommentService = pullRequestCommentService;
        WorkItemService = workItemService;
        PullRequestStateChangeService = pullRequestStateChangeService;
        _teamMemberService = teamMemberService;
    }

    public async Task LoadTeamMembers()
    {
        TeamMembers.Clear();
        TeamMembers.Add("Loading...");

        var members = await _teamMemberService.GetCustomerOptimizationTeamMembers();

        TeamMembers.Clear();
        foreach (var member in members.OrderBy(m => m.Identity.DisplayName))
        {
            TeamMembers.Add(member.Identity.DisplayName);
        }
    }

    public async Task LoadMemberData(string memberName, int numberOfDaysToFetch)
    {
        // Load Comment Stats for member
        var allCommentData = await PullRequestCommentService.ShowCommentCounts(numberOfDaysToFetch);
        if (allCommentData.TryGetValue(memberName, out var reviewCounts))
        {
            _commentData[memberName] = reviewCounts;
            _commentLinks[memberName] = PullRequestCommentService.GetDeveloperCommentLinks(memberName);
        }
        else
        {
            _commentData.Remove(memberName);
            _commentLinks.Remove(memberName);
        }

        // Load Work Item Stats for member
        var allWorkItems =
            (await WorkItemService.GetWorkItemsWithDeveloperReferenceSinceDate(
                DateTime.Today.Subtract(TimeSpan.FromDays(numberOfDaysToFetch)))).ToList();

        var memberWorkItems = allWorkItems.FirstOrDefault(wi => wi.DeveloperName == memberName);
        if (memberWorkItems != null)
        {
            _workItemData[memberName] = memberWorkItems.WorkItems.ToList();
        }
        else
        {
            _workItemData.Remove(memberName);
        }

        // Load PR Stats for member
        var opened = await PullRequestStateChangeService.ShowOpenedPrCounts(numberOfDaysToFetch);
        var openedDict = opened.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (openedDict.TryGetValue(memberName, out var openedCount))
        {
            _prOpenedData[memberName] = openedCount;
        }
        else
        {
            _prOpenedData.Remove(memberName);
        }

        var closed = await PullRequestStateChangeService.ShowClosedPrCounts(numberOfDaysToFetch);
        var closedDict = closed.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (closedDict.TryGetValue(memberName, out var closedCount))
        {
            _prClosedData[memberName] = closedCount;
        }
        else
        {
            _prClosedData.Remove(memberName);
        }

        var reviewed = await PullRequestStateChangeService.ShowReviewedPrCounts(numberOfDaysToFetch);
        var reviewedDict = reviewed.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (reviewedDict.TryGetValue(memberName, out var reviewedCount))
        {
            _prReviewedData[memberName] = reviewedCount;
        }
        else
        {
            _prReviewedData.Remove(memberName);
        }
    }

    public FSharp.Models.ReviewCounts? GetCommentStats(string memberName)
    {
        return _commentData.TryGetValue(memberName, out var counts) ? counts : null;
    }

    public List<FSharp.Models.Link> GetCommentLinks(string memberName)
    {
        return _commentLinks.TryGetValue(memberName, out var links) ? links : new List<FSharp.Models.Link>();
    }

    public List<WorkItem> GetWorkItems(string memberName)
    {
        return _workItemData.TryGetValue(memberName, out var items) ? items : new List<WorkItem>();
    }

    public int GetPrOpenedCount(string memberName)
    {
        return _prOpenedData.TryGetValue(memberName, out var count) ? count : 0;
    }

    public int GetPrClosedCount(string memberName)
    {
        return _prClosedData.TryGetValue(memberName, out var count) ? count : 0;
    }

    public int GetPrReviewedCount(string memberName)
    {
        return _prReviewedData.TryGetValue(memberName, out var count) ? count : 0;
    }
}
