using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Stats2fa.api.models;
using Stats2fa.logger;

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
        Distributors response;

        var url = $"accounts/distributors?owner=00000000-0000-0000-0000-000000000000&offset={distributors.DistributorList.Count}&limit={limit}&sort=name&filter=state!=deleted";

        try {
            // Use GetAsync instead of GetFromJsonAsync for more control over response handling
            var httpResponse = await client.GetAsync(requestUri: url);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting distributors. URL: {client.BaseAddress}{url}");
                return distributors; // Return what we have so far
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType} getting distributors. URL: {client.BaseAddress}{url}");
                return distributors; // Return what we have so far
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Distributors>() ?? new Distributors();
        }
        catch (JsonException jsonEx) {
            StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting distributors. URL: {client.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: jsonEx.Message);
            return distributors; // Return what we have so far
        }
        catch (TaskCanceledException tcEx) {
            StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation getting distributors. URL: {client.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: tcEx.Message);
            return distributors; // Return what we have so far
        }
        catch (Exception e) {
            StatsLogger.Log(stats: apiInformation, $"Error getting distributors. URL: {client.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: e.Message);
            return distributors; // Return what we have so far
        }

        distributors.Count = response.Count;
        distributors.Limit = response.Limit;
        distributors.DistributorList.AddRange(collection: response.DistributorList);

        if (distributors.DistributorList.Count < response.Count && distributors.DistributorList.Count < maxResults)
            return await GetDistributors(client: client, apiInformation: apiInformation, distributors: distributors, limit: limit, maxResults: maxResults);

        return distributors;
    }

    internal static async ValueTask GetVendorsForDistributor(ConcurrentBag<Vendor> result, HttpClient httpClient, ApiInformation apiInformation, Distributor distributor, Vendors? vendors, CancellationToken cancellationToken, int limit = 1000, int maxResults = 1000) {
        vendors ??= new Vendors {
            Offset = 0,
            Limit = 0,
            Count = 0,
            VendorList = new List<Vendor>()
        };
        apiInformation.ApiCallsVendors++;
        apiInformation.LastUpdated = DateTime.UtcNow;
        Vendors response;

        var url = $"accounts/vendors?owner={distributor.Id}&offset={vendors.VendorList.Count}&limit={limit}&sort=name&filter=state!=deleted";

        try {
            // Use GetAsync instead of GetFromJsonAsync for more control over response handling
            var httpResponse = await httpClient.GetAsync(requestUri: url, cancellationToken: cancellationToken);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting vendors for distributor {distributor.Id}. URL: {httpClient.BaseAddress}{url}");

                // Just return the current vendors without throwing an exception
                if (vendors.VendorList.Count > 0)
                    foreach (var vendor in vendors.VendorList)
                        result.Add(item: vendor);

                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType} for distributor. URL: {httpClient.BaseAddress}{url}", distributor: distributor.Id);

                // Just return the current vendors without throwing an exception
                if (vendors.VendorList.Count > 0)
                    foreach (var vendor in vendors.VendorList)
                        result.Add(item: vendor);

                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Vendors>(cancellationToken: cancellationToken);

            if (response == null) {
                StatsLogger.Log(stats: apiInformation, $"Null response getting vendors for distributor {distributor.Id}. URL: {httpClient.BaseAddress}{url}");

                // Just return the current vendors without throwing an exception
                if (vendors.VendorList.Count > 0)
                    foreach (var vendor in vendors.VendorList)
                        result.Add(item: vendor);

                return;
            }
        }
        catch (JsonException jsonEx) {
            StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting vendors for distributor {distributor.Id}. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: jsonEx.Message);

            // Just return the current vendors without throwing an exception
            if (vendors.VendorList.Count > 0)
                foreach (var vendor in vendors.VendorList)
                    result.Add(item: vendor);

            return;
        }
        catch (TaskCanceledException tcEx) {
            StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation getting vendors for distributor {distributor.Id}. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: tcEx.Message);

            // Just return the current vendors without throwing an exception
            if (vendors.VendorList.Count > 0)
                foreach (var vendor in vendors.VendorList)
                    result.Add(item: vendor);

            return;
        }
        catch (Exception e) {
            StatsLogger.Log(stats: apiInformation, $"Error getting vendors for distributor {distributor.Id}. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: e.Message);

            // Just return the current vendors without throwing an exception
            if (vendors.VendorList.Count > 0)
                foreach (var vendor in vendors.VendorList)
                    result.Add(item: vendor);

            return;
        }

        vendors.Count = response.Count;
        vendors.Limit = response.Limit;
        vendors.VendorList.AddRange(collection: response.VendorList);

        if (vendors.VendorList.Count < response.Count && vendors.VendorList.Count < maxResults)
            await GetVendorsForDistributor(result: result, httpClient: httpClient, apiInformation: apiInformation, distributor: distributor, vendors: vendors, cancellationToken: cancellationToken, limit: limit, maxResults: maxResults);
        else
            foreach (var vendor in vendors.VendorList)
                result.Add(item: vendor);
    }

    internal static async ValueTask GetClientsForVendor(ConcurrentBag<Client> result, HttpClient httpClient, ApiInformation apiInformation, Vendor vendor, Clients? clients, CancellationToken cancellationToken, int limit = 1000, int maxResults = 1000) {
        clients ??= new Clients {
            Offset = 0,
            Limit = 0,
            Count = 0,
            ClientList = new List<Client>()
        };
        apiInformation.ApiCallsClients++;
        apiInformation.LastUpdated = DateTime.UtcNow;
        Clients response;

        var url = $"accounts/clients?owner={vendor.Id}&offset={clients.ClientList.Count}&limit={limit}&sort=name%3Aasc&filter=(state%3Dinactive%7Cstate%3Dactive%7Cstate%3Dsuspended)";

        try {
            // Use GetAsync instead of GetFromJsonAsync for more control over response handling
            var httpResponse = await httpClient.GetAsync(requestUri: url, cancellationToken: cancellationToken);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting clients for vendor {vendor.Id}. URL: {httpClient.BaseAddress}{url}");

                // Just return the current clients without throwing an exception
                if (clients.ClientList.Count > 0)
                    foreach (var client in clients.ClientList)
                        result.Add(item: client);

                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType} for vendor. URL: {httpClient.BaseAddress}{url}", vendor: vendor.Id);

                // Just return the current clients without throwing an exception
                if (clients.ClientList.Count > 0)
                    foreach (var client in clients.ClientList)
                        result.Add(item: client);

                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Clients>(cancellationToken: cancellationToken);
        }
        catch (JsonException jsonEx) {
            StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting clients for vendor {vendor.Id}. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: jsonEx.Message);

            // Just return the current clients without throwing an exception
            if (clients.ClientList.Count > 0)
                foreach (var client in clients.ClientList)
                    result.Add(item: client);

            return;
        }
        catch (TaskCanceledException tcEx) {
            StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation getting clients for vendor {vendor.Id}. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: tcEx.Message);

            // Just return the current clients without throwing an exception
            if (clients.ClientList.Count > 0)
                foreach (var client in clients.ClientList)
                    result.Add(item: client);

            return;
        }
        catch (Exception e) {
            StatsLogger.Log(stats: apiInformation, $"Error getting clients for vendor {vendor.Id}. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: e.Message);

            // Just return the current clients without throwing an exception
            if (clients.ClientList.Count > 0)
                foreach (var client in clients.ClientList)
                    result.Add(item: client);

            return;
        }

        if (response == null) {
            StatsLogger.Log(stats: apiInformation, $"[{DateTime.UtcNow:s}][   ][{vendor.owner.Id}][{vendor.Id}][{Guid.Empty}] Error fetching clients ({clients.ClientList.Count:00000})");

            // Just return the current clients without throwing an exception
            if (clients.ClientList.Count > 0)
                foreach (var client in clients.ClientList)
                    result.Add(item: client);
        }
        else {
            clients.Count = response.Count;
            clients.Limit = response.Limit;
            clients.ClientList.AddRange(collection: response.ClientList);

            if (clients.ClientList.Count < response.Count && clients.ClientList.Count < maxResults)
                await GetClientsForVendor(result: result, httpClient: httpClient, apiInformation: apiInformation, vendor: vendor, clients: clients, cancellationToken: cancellationToken, limit: limit, maxResults: maxResults);
            else
                foreach (var client in clients.ClientList)
                    result.Add(item: client);
        }
    }
}