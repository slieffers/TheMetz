// using Microsoft.TeamFoundation.SourceControl.WebApi;
// using System.Collections.Concurrent;
// using Microsoft.VisualStudio.Services.Common;
// using Microsoft.VisualStudio.Services.WebApi;
// using TheMetz.Interfaces;
//
// namespace TheMetz.Services
// {
//     internal class PullRequestCommentService : IPullRequestCommentService
//     {
//         private readonly VssConnection _connection;
//         private ConcurrentDictionary<string, List<(string Title, string Url)>> _developerCommentLinks = new();
//         private readonly IPullRequestService _pullRequestService;
//         private readonly ITeamMemberService _teamMemberService;
//
//         public PullRequestCommentService(VssConnection connection, IPullRequestService pullRequestService, ITeamMemberService teamMemberService)
//         {
//             _connection = connection;
//             _pullRequestService = pullRequestService;
//             _teamMemberService = teamMemberService;
//         }
//
//         public async Task<IEnumerable<KeyValuePair<string, (int, int)>>> ShowCommentCounts(int numberOfDays)
//         {
//             DateTime fromDate = DateTime.Today.AddDays(-numberOfDays);
//
//             List<TeamMember>? allTeamMembers= await _teamMemberService.GetCustomerOptimizationTeamMembers();
//             var memberNames = new ConcurrentBag<string>();
//             allTeamMembers!.Select(t => t.Identity.DisplayName).ForEach(m => memberNames.Add(m));
//             
//             List<GitPullRequest> pullRequests = await _pullRequestService.GetPullRequestsByDateOpenedOrClosed(numberOfDays);
//
//             var developerCommentCount = new ConcurrentDictionary<string, (int, int)>();
//             _developerCommentLinks = new ConcurrentDictionary<string, List<(string, string)>>();
//
//             List<GitPullRequest> filteredPullRequests = pullRequests
//                 .Where(pr =>
//                     pr.SourceRefName != "refs/heads/develop"
//                     && pr.SourceRefName != "refs/heads/Test"
//                     && pr.CreatedBy.DisplayName != "Project Collection Build Service (DefaultCollection)"
//                     && pr.Reviewers.Any(r => memberNames.Contains(r.DisplayName))).ToList();
//
//             Dictionary<string, int> perAuthorTotalReviews = new Dictionary<string, int>();
//             foreach (string memberName in memberNames)
//             {
//                 perAuthorTotalReviews.Add(memberName, filteredPullRequests.Count(pr => 
//                     pr.Reviewers.Any(r => memberName == r.DisplayName) && pr.CreatedBy.DisplayName != memberName));
//             }
//             
//             using var gitClient = await _connection.GetClientAsync<GitHttpClient>();
//
//             await Parallel.ForEachAsync(filteredPullRequests, new ParallelOptions
//             {
//                 MaxDegreeOfParallelism = Environment.ProcessorCount // Adjust as needed
//             }, async (pr, cancellationToken) =>
//             {
//                 try
//                 {
//                     List<GitPullRequestCommentThread> threads = (await gitClient.GetThreadsAsync(
//                         repositoryId: pr.Repository.Id,
//                         pullRequestId: pr.PullRequestId,
//                         cancellationToken: cancellationToken
//                     )).ToList();
//
//                     List<string> authorComments = threads.SelectMany(t => t.Comments)
//                         .Where(c => c.CommentType != CommentType.System && c.PublishedDate >= fromDate)
//                         .GroupBy(c => c.Author.DisplayName)
//                         .Select(c => c.Key).ToList();
//
//                     foreach (string? authorComment in authorComments)
//                     {
//                         if (authorComment == pr.CreatedBy.DisplayName
//                             || !memberNames.Contains(authorComment))
//                         {
//                             continue;
//                         }
//
//                         if (_developerCommentLinks.TryGetValue(authorComment,
//                                 out List<(string Title, string Url)>? value)
//                             && value.Any(r => r.Title == pr.Title))
//                         {
//                             continue;
//                         }
//
//                         developerCommentCount.AddOrUpdate(
//                             authorComment,
//                             (perAuthorTotalReviews[authorComment], 1),
//                             (string _, (int totalReviews, int commentCount)count) => (count.totalReviews, count.commentCount + 1)
//                         );
//                         _developerCommentLinks.AddOrUpdate(
//                             authorComment,
//                             _ => new List<(string, string)> { (pr.Title, GetFormattedPrUrl(pr)) },
//                             (_, existingList) =>
//                             {
//                                 existingList.Add((pr.Title, GetFormattedPrUrl(pr)));
//                                 return existingList;
//                             }
//                         );
//                     }
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine(e);
//                 }
//             });
//             
//             return (developerCommentCount);
//         }
//         
//         public List<(string Title, string Url)> GetDeveloperCommentLinks(string developerName)
//         {
//             return _developerCommentLinks[developerName].ToList();
//         }
//         
//         private static string GetFormattedPrUrl(GitPullRequest pr)
//         {
//             return
//                 $"https://tfs.clarkinc.biz/DefaultCollection/{pr.Repository.ProjectReference.Name}/_git/{pr.Repository.Name}/pullrequest/{pr.PullRequestId}";
//         }
//     }
// }