using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Stats2fa.api.models;

namespace Stats2fa.api;

internal class ApiUtils {
    internal static async Task<Distributors> GetDistributors(HttpClient client, ApiInformation apiInformation, Distributors? distributors, int limit = 1000, int maxResults = 1000) {
        distributors ??= new Distributors {
            Offset = 0,
            Limit = 0,
            Count = 0,
            DistributorList = new List<Distributor>()
        };
        apiInformation.ApiCallsDistributors++;
        apiInformation.LastUpdated = DateTime.UtcNow;
        var response = await client.GetFromJsonAsync<Distributors>($"accounts/distributors?owner=00000000-0000-0000-0000-000000000000&offset={distributors.DistributorList.Count}&limit={limit}&sort=name&filter=state!=deleted") ?? new Distributors();
        distributors.Count = response.Count;
        response.Limit = response.Limit;
        distributors.DistributorList.AddRange(response.DistributorList);
        if (distributors.DistributorList.Count < response.Count && distributors.DistributorList.Count < maxResults) return await GetDistributors(client, apiInformation, distributors, limit: limit, maxResults: maxResults);
        return distributors;
    }
}