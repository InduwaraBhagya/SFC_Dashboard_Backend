using SFCDashboard.Models;
using System.Text.Json;

namespace SFCDashboard.ApiClients
{
    public class CustomerUserAssignmentsApiClient : ICustomerUserAssignmentsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public CustomerUserAssignmentsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<IEnumerable<CustomerUserAssignment>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/customeruserassignments");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<CustomerUserAssignment>>(json, _jsonOptions) ?? new List<CustomerUserAssignment>();
        }

        public async Task<CustomerUserAssignment?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/customeruserassignments/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CustomerUserAssignment>(json, _jsonOptions);
        }

        public async Task<CustomerUserAssignment> CreateAsync(CustomerUserAssignment customerUserAssignment)
        {
            var json = JsonSerializer.Serialize(customerUserAssignment, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/customeruserassignments", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CustomerUserAssignment>(responseJson, _jsonOptions)!;
        }

        public async Task<CustomerUserAssignment> UpdateAsync(CustomerUserAssignment customerUserAssignment)
        {
            var json = JsonSerializer.Serialize(customerUserAssignment, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/customeruserassignments/{customerUserAssignment.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CustomerUserAssignment>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/customeruserassignments/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<CustomerUserAssignment>> SearchAssignmentsAsync(string searchTerm)
        {
            var response = await _httpClient.GetAsync($"api/customeruserassignments/search?searchTerm={Uri.EscapeDataString(searchTerm)}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<CustomerUserAssignment>>(json, _jsonOptions) ?? new List<CustomerUserAssignment>();
        }

        public async Task<IEnumerable<string>> GetAvailableCustomersAsync(List<string> assignedCustomers)
        {
            var assignedCustomersJson = JsonSerializer.Serialize(assignedCustomers, _jsonOptions);
            var content = new StringContent(assignedCustomersJson, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/customeruserassignments/available-customers", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<string>>(json, _jsonOptions) ?? new List<string>();
        }

        public async Task<CustomerUserAssignment?> GetExistingAssignmentAsync(string customer, int userId)
        {
            var response = await _httpClient.GetAsync($"api/customeruserassignments/existing?customer={Uri.EscapeDataString(customer)}&userId={userId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CustomerUserAssignment>(json, _jsonOptions);
        }

        public async Task<CustomerUserAssignment?> GetExistingAssignmentByCustomerAsync(string customer)
        {
            var response = await _httpClient.GetAsync($"api/customeruserassignments/by-customer?customer={Uri.EscapeDataString(customer)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CustomerUserAssignment>(json, _jsonOptions);
        }

        public async Task<IEnumerable<CustomerUserAssignment>> GetAssignmentsWithUsersAsync()
        {
            var response = await _httpClient.GetAsync("api/customeruserassignments/with-users");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<CustomerUserAssignment>>(json, _jsonOptions) ?? new List<CustomerUserAssignment>();
        }
    }
}
