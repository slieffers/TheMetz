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
    private Dictionary<string, List<(string Title, string Url)>> _prOpenedLinks = new();
    private Dictionary<string, List<(string Title, string Url)>> _prClosedLinks = new();
    private Dictionary<string, List<(string Title, string Url)>> _prReviewedLinks = new();

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

        // Load PR link details for member
        await PullRequestStateChangeService.ShowOpenedPrCounts(numberOfDaysToFetch);
        var openedLinks = PullRequestStateChangeService.GetDeveloperOpenedPrLinks(memberName);
        if (openedLinks.Any())
        {
            _prOpenedLinks[memberName] = openedLinks;
        }
        else
        {
            _prOpenedLinks.Remove(memberName);
        }

        await PullRequestStateChangeService.ShowClosedPrCounts(numberOfDaysToFetch);
        var closedLinks = PullRequestStateChangeService.GetDeveloperClosedPrLinks(memberName);
        if (closedLinks.Any())
        {
            _prClosedLinks[memberName] = closedLinks;
        }
        else
        {
            _prClosedLinks.Remove(memberName);
        }

        await PullRequestStateChangeService.ShowReviewedPrCounts(numberOfDaysToFetch);
        var reviewedLinks = PullRequestStateChangeService.GetDeveloperReviewedPrLinks(memberName);
        if (reviewedLinks.Any())
        {
            _prReviewedLinks[memberName] = reviewedLinks;
        }
        else
        {
            _prReviewedLinks.Remove(memberName);
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

    public List<(string Title, string Url)> GetPrOpenedLinks(string memberName)
    {
        return _prOpenedLinks.TryGetValue(memberName, out var links) ? links : new List<(string Title, string Url)>();
    }

    public List<(string Title, string Url)> GetPrClosedLinks(string memberName)
    {
        return _prClosedLinks.TryGetValue(memberName, out var links) ? links : new List<(string Title, string Url)>();
    }

    public List<(string Title, string Url)> GetPrReviewedLinks(string memberName)
    {
        return _prReviewedLinks.TryGetValue(memberName, out var links) ? links : new List<(string Title, string Url)>();
    }
}
