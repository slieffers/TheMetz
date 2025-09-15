using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using TheMetz.FSharp;
using TheMetz.Interfaces;
using TheMetz.Models;
using TheMetz.Repositories;
using TheMetz.Services;
//using IPullRequestService = TheMetz.Services.IPullRequestService;

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
        services.AddSingleton<IPullRequestService>(sp =>
            new TheMetz.FSharp.PullRequestService(
                sp.GetRequiredService<VssConnection>(),
                sp.GetRequiredService<IPrRepository>(),
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            ));
        services.AddSingleton<ITeamMemberService, TeamMemberService>();
        services.AddSingleton<IPullRequestStateChangeService, PullRequestStateChangeService>();
        services.AddSingleton<IWorkItemService, WorkItemService>();
        services.AddSingleton<IPrRepository, PrRepository>();
        services.AddSingleton<IWorkItemRepository, WorkItemRepository>();
        services.AddTransient<PullRequestStatsViewModel>();
        services.AddTransient<CommentStatsViewModel>();
        services.AddTransient<WorkItemStatsViewModel>();

        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }
}