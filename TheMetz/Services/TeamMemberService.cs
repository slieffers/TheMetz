using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Concurrent;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Graph.Client;

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
            (List<string> CustomerOptimizationTeamMembers, List<string> OtherTeamMembers) allTeamMembers = GetAllTeamMembersAsync();
            
            var teamClient = await _connection.GetClientAsync<TeamHttpClient>();

            // Retrieve the team members
            List<TeamMember>? members = await teamClient.GetTeamMembersWithExtendedPropertiesAsync(
                projectId: "Marketplace",
                teamId: "Customer Optimization"
            );
            
            return members;
        }
        
        private static (List<string> CustomerOptimizationTeamMembers, List<string> OtherTeamMembers) GetAllTeamMembersAsync()
        {
            var customerOptimizationTeamMembers = new List<string>
            {
                "Dylan Manning",
                "David Acker",
                "Kerry Hannigan",
                "Liudmila Solovyeva",
                "Andrew Chanthavisith",
                "Michal Lesniewski",
                "Khrystyna Kregenbild"
            };
            var otherTeamMembers = new List<string>
            {
                "Phil Gathany",
                "Brandon George",
                "Shane Lieffers",
                "Shawn Dreier"
            };
            
            return (customerOptimizationTeamMembers, otherTeamMembers);
        }
    }
}