using Microsoft.VisualStudio.Services.WebApi;

namespace TheMetz.Interfaces;

public interface ITeamMemberService
{
    public Task<List<TeamMember>> GetCustomerOptimizationTeamMembers();
}