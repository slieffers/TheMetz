using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using TheMetz.Models;
using TheMetz.Repositories;
using TheMetz.Services;

namespace TheMetz;

public static class Startup
{
    public static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        IConfigurationRoot configurationRoot = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        services.AddSingleton<IConfiguration>(configurationRoot);

        var appSettings = configurationRoot.Get<AppSettings>()!;
        services.AddSingleton(appSettings);

        var adoConnection = new VssConnection(new Uri(appSettings.OrganizationUrl), new VssBasicCredential(string.Empty, appSettings.PersonalAccessToken));
        services.AddSingleton(adoConnection);
        services.AddSingleton<IPullRequestCommentService, PullRequestCommentService>();
        services.AddSingleton<IPullRequestService, PullRequestService>();
        services.AddSingleton<ITeamMemberService, TeamMemberService>();
        services.AddSingleton<IPullRequestStateChangeService, PullRequestStateChangeService>();
        services.AddSingleton<IWorkItemService, WorkItemService>();
        services.AddSingleton<IPrRepository, PrRepository>();
        services.AddSingleton<IWorkItemRepository, WorkItemRepository>();
        services.AddTransient<PullRequestStatsViewModel>();
        services.AddTransient<CommentStatsViewModel>();

        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }
}