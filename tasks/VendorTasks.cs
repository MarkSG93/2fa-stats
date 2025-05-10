using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Stats2fa.api;
using Stats2fa.api.models;
using Stats2fa.database;
using Stats2fa.utils;

namespace Stats2fa.tasks;

internal class VendorTasks {
    private static List<VendorInformation> FetchUnprocessedVendors(StatsContext db, int pageSize, DateTime reportDate) {
        var pageIndex = 1;
        var vendors = db.Vendors
            .Where(x => x.CreatedTimestamp < reportDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);
        return vendors.ToList();
    }

    private static async ValueTask GetVendorInformation(HttpClient httpClient, ApiInformation apiInformation, VendorInformation vendor, CancellationToken cancellationToken) {
        var tasks = new List<Task> {
            GetVendorInformationAndSettings(httpClient, apiInformation, vendor), 
            // GetVendorStats(httpClient, apiInformation, vendor),
        };
        await Task.WhenAll(tasks);
        vendor.CreatedTimestamp = DateTime.UtcNow; // update the CreatedTimestamp now we have all the info
    }

    private static async Task GetVendorInformationAndSettings(HttpClient client, ApiInformation apiInformation, VendorInformation vendorInformation) {
        apiInformation.ApiCallsVendors++;
        apiInformation.LastUpdated = DateTime.UtcNow;

        string url = $"accounts/vendors/{vendorInformation.VendorId}";
        Vendor response;

        try {
            // Use GetAsync instead of GetFromJsonAsync for more robust error handling
            var httpResponse = await client.GetAsync(url);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                Console.WriteLine($"HTTP error {httpResponse.StatusCode} getting vendor {vendorInformation.VendorId}. URL: {client.BaseAddress}{url}");
                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                Console.WriteLine($"Unexpected content type: {contentType} for vendor {vendorInformation.VendorId}. URL: {client.BaseAddress}{url}");
                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Vendor>() ?? new Vendor();
        }
        catch (System.Text.Json.JsonException jsonEx) {
            Console.WriteLine($"JSON parsing error getting vendor {vendorInformation.VendorId}. URL: {client.BaseAddress}{url}");
            Console.WriteLine(jsonEx.Message);
            return;
        }
        catch (TaskCanceledException tcEx) {
            Console.WriteLine($"Request timeout or cancellation getting vendor {vendorInformation.VendorId}. URL: {client.BaseAddress}{url}");
            Console.WriteLine(tcEx.Message);
            return;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error getting vendor {vendorInformation.VendorId}. URL: {client.BaseAddress}{url}");
            Console.WriteLine(ex.Message);
            return;
        }

        List<string> names = new List<string>();
        if (response.AvailableMapSets != null) {
            foreach (var item in response.AvailableMapSets) {
                if (item != null && item.Name != null) {
                    names.Add(item.Name);
                }
            }
        }

        // Safely set properties with null checks to avoid NullReferenceException
        try {
            if (response.passwordPolicy != null) {
                if (response.passwordPolicy.Source != null) {
                    vendorInformation.VendorPasswordPolicySourceId = response.passwordPolicy.Source.Id;
                    vendorInformation.VendorPasswordPolicySourceName = response.passwordPolicy.Source.Name;
                    vendorInformation.VendorPasswordPolicySourceType = response.passwordPolicy.Source.Type;
                }

                vendorInformation.VendorPasswordPolicyPasswordLength = response.passwordPolicy.PasswordLength;

                if (response.passwordPolicy.PasswordComplexity != null) {
                    vendorInformation.VendorPasswordPolicyPasswordComplexityMixedcase = response.passwordPolicy.PasswordComplexity.MixedCase;
                    vendorInformation.VendorPasswordPolicyPasswordComplexityAlphanumerical = response.passwordPolicy.PasswordComplexity.AlphaNumerical;
                    vendorInformation.VendorPasswordPolicyPasswordComplexityNocommonpasswords = response.passwordPolicy.PasswordComplexity.NoCommonPasswords;
                    vendorInformation.VendorPasswordPolicyPasswordComplexitySpecialcharacters = response.passwordPolicy.PasswordComplexity.SpecialCharacters;
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(response.passwordPolicy));
                }

                vendorInformation.VendorPasswordPolicyPasswordExpirationDays = response.passwordPolicy.PasswordExpirationDays;

                if (response.passwordPolicy.OtpSettings != null) {
                    if (response.passwordPolicy.OtpSettings.Methods != null) {
                        if (response.passwordPolicy.OtpSettings.Methods.Totp != null) {
                            vendorInformation.VendorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Totp.TokenValidityDays;
                        }

                        if (response.passwordPolicy.OtpSettings.Methods.Email != null) {
                            vendorInformation.VendorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Email.TokenValidityDays;
                        }
                    }

                    vendorInformation.VendorPasswordPolicyOtpSettingsGracePeriodDays = response.passwordPolicy.OtpSettings.GracePeriodDays;
                    vendorInformation.VendorPasswordPolicyOtpSettingsMandatoryFor = response.passwordPolicy.OtpSettings.MandatoryFor;
                }
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error processing vendor data for {vendorInformation.VendorId}: {ex.Message}");
        }
    }

    
    public static async Task PopulateVendorInformation(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int counter = 0) {
        try {
            var pageSize = 10;
            Console.WriteLine($"Fetching vendors < {reportDate:s}");
            List<VendorInformation> vendors = FetchUnprocessedVendors(db, pageSize, reportDate);
            Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"fetching {vendors.Count} vendors to update"));

            if (vendors.Any()) {
                try {
                    await Parallel.ForEachAsync(source: vendors, (vendor, cancellationToken) =>
                        GetVendorInformation(httpClient, apiInformation, vendor, cancellationToken));
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error during vendor information fetching: {ex.Message}");
                    // Continue with saving what we have
                }

                try {
                    await db.SaveChangesAsync();
                    counter += vendors.Count;
                    Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation,
                        $"checkpointed {vendors.Count} vendors"));
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error saving vendor information to database: {ex.Message}");
                }

                // Process the next batch
                await PopulateVendorInformation(httpClient, apiInformation, db, reportDate, counter);
            }
            else {
                Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation,
                    $"PopulateVendorInformation complete {counter} prepared vendors"));
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Unhandled exception in PopulateVendorInformation: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}