using System.Net.Http;
using System.Net.Http.Headers;

public class AzureDevOpsService
{
    private readonly string _baseAddress = "https://tfs.clarkinc.biz/DefaultCollection/Marketplace";
    private readonly string _project = "Marketplace"; // Replace with your project name
    private readonly string _repository = "Marketplace"; // Replace with your repository name
    private readonly string _personalAccessToken = "j3dcue4ijcpz6qxmdzb6uvdm6t6bgxaw2d3tcn6ymswbvuv7y7ra";

    // Fetch pull requests
    public async Task<string> GetPullRequestsAsync()
    {
        string endpoint = $"{_baseAddress}/{_project}/_apis/git/repositories/{_repository}/pullrequests?api-version=7.0";

        using (var client = new HttpClient())
        {
            // Add Authorization header
            var basicAuthHeaderValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{_personalAccessToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthHeaderValue);

            // Make the GET request
            var response = await client.GetAsync(endpoint);

            // Ensure the response is successful
            response.EnsureSuccessStatusCode();

            // Parse the response as JSON
            var jsonResponse = await response.Content.ReadAsStringAsync();

            return jsonResponse; // Returning raw JSON. You can post-process it as needed.
        }
    }
}