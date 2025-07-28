using SFCDashboard.Models;

namespace SFCDashboard.ApiClients
{
    public class SubTaskListsApiClient : ISubTaskListsApiClient
    {
        private readonly HttpClient _httpClient;
        public SubTaskListsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SubTaskList> GetByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<SubTaskList>($"/api/subtasklists/{id}");
        }

        public async Task<IEnumerable<SubTaskList>> GetByTaskListIdAsync(int taskListId)
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<SubTaskList>>($"/api/subtasklists/bytasklist/{taskListId}");
        }

        public async Task<SubTaskList> GetByTaskListIdAndNameAsync(int taskListId, string subTaskName)
        {
            return await _httpClient.GetFromJsonAsync<SubTaskList>($"/api/subtasklists/bytasklistandname/{taskListId}?name={System.Net.WebUtility.UrlEncode(subTaskName)}");
        }

        public async Task AddAsync(SubTaskList subTaskList)
        {
            await _httpClient.PostAsJsonAsync("/api/subtasklists", subTaskList);
        }

        public async Task UpdateAsync(SubTaskList subTaskList)
        {
            await _httpClient.PutAsJsonAsync($"/api/subtasklists/{subTaskList.Id}", subTaskList);
        }
    }
}
