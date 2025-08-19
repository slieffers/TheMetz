using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace TheMetz.Services
{
    public interface ITeamMemberService
    {
        public Task<List<TeamMember>?> GetCustomerOptimizationTeamMembers();
    }
    
    internal class TeamMemberService : ITeamMemberService
    {
        private readonly VssConnection _connection;
        
        public TeamMemberService(VssConnection connection)
        {
            _connection = connection;
        }
        
        public async Task<List<TeamMember>?> GetCustomerOptimizationTeamMembers()
        {
            var teamClient = await _connection.GetClientAsync<TeamHttpClient>();
            var test = await _connection.GetClientAsync<WorkItemTrackingHttpClient>();
                
            // Retrieve the team members
            List<TeamMember>? members = await teamClient.GetTeamMembersWithExtendedPropertiesAsync(
                projectId: "Marketplace",
                teamId: "Customer Optimization"
            );
            
            // var bizopsTeamMembers = await teamClient.GetTeamMembersWithExtendedPropertiesAsync(
            //     projectId: "Marketplace",
            //     teamId: "Business Optimization"
            // );
            //
            // members.Add(bizopsTeamMembers.First(t => t.Identity.DisplayName == "Brandon George" ));
            // members.Add(bizopsTeamMembers.First(t => t.Identity.DisplayName == "Phil Gathany" ));
            
            return members;
        }
    }
}