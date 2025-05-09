using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
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
    
    internal static async ValueTask GetVendorsForDistributor(ConcurrentBag<Vendor> result, HttpClient httpClient, ApiInformation apiInformation, Distributor distributor, Vendors? vendors, CancellationToken cancellationToken, int limit = 1000, int maxResults = 1000) {
        vendors ??= new Vendors {
            Offset = 0,
            Limit = 0,
            Count = 0,
            VendorList = new List<Vendor>()
        };
        apiInformation.ApiCallsVendors++;
        apiInformation.LastUpdated = DateTime.UtcNow;
        var response = await httpClient.GetFromJsonAsync<Vendors>($"accounts/vendors?owner={distributor.Id}&offset={vendors.VendorList.Count}&limit={limit}&sort=name&filter=state!=deleted", cancellationToken);
        vendors.Count = response.Count;
        vendors.Limit = response.Limit;
        vendors.VendorList.AddRange(response.VendorList);
        if (vendors.VendorList.Count < response.Count && vendors.VendorList.Count < maxResults) {
            await GetVendorsForDistributor(result, httpClient, apiInformation, distributor, vendors, cancellationToken, limit: limit, maxResults: maxResults);
        } else {
            foreach (var vendor in vendors.VendorList) {
                result.Add(vendor);
            }
        }
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
        try {
            response = await httpClient.GetFromJsonAsync<Clients>($"accounts/clients?owner={vendor.Id}&offset={clients.ClientList.Count}&limit={limit}&sort=name%3Aasc&filter=(state%3Dinactive%7Cstate%3Dactive%7Cstate%3Dsuspended)", cancellationToken);
        } 
        catch (Exception e) {
            Console.WriteLine($"error getting {httpClient.BaseAddress}accounts/clients?owner={vendor.Id}&offset={clients.ClientList.Count}&limit={limit}&sort=name%3Aasc&filter=(state%3Dinactive%7Cstate%3Dactive%7Cstate%3Dsuspended)");
            Console.WriteLine(e);
            throw;
        }
        
        if (response == null) {
            Console.WriteLine($"[{DateTime.UtcNow:s}][   ][{vendor.owner.Id}][{vendor.Id}][{Guid.Empty}] Error fetching clients ({clients.ClientList.Count:00000})");
        } else {
            clients.Count = response.Count;
            clients.Limit = response.Limit;
            clients.ClientList.AddRange(response.ClientList);
            if (clients.ClientList.Count < response.Count && clients.ClientList.Count < maxResults) {
                await GetClientsForVendor(result, httpClient, apiInformation, vendor, clients, cancellationToken, limit: limit, maxResults: maxResults);
            } else {
                foreach (var client in clients.ClientList) {
                    result.Add(client);
                }
            }
        }
    }
}