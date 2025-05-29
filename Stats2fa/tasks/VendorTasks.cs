using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Stats2fa.api;
using Stats2fa.api.models;
using Stats2fa.database;
using Stats2fa.logger;

namespace Stats2fa.tasks;

internal class VendorTasks {
    private static List<VendorInformation> FetchUnprocessedVendors(StatsContext db, int pageSize, DateTime reportDate) {
        var pageIndex = 1;
        var vendors = db.Vendors
            .Where(x => x.CreatedTimestamp < reportDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(count: pageSize);
        return vendors.ToList();
    }

    private static async ValueTask GetVendorInformation(HttpClient httpClient, ApiInformation apiInformation, VendorInformation vendor, CancellationToken cancellationToken) {
        var tasks = new List<Task> {
            GetVendorInformationAndSettings(client: httpClient, apiInformation: apiInformation, vendorInformation: vendor)
            // GetVendorStats(httpClient, apiInformation, vendor),
        };
        await Task.WhenAll(tasks: tasks);
        vendor.CreatedTimestamp = DateTime.UtcNow; // update the CreatedTimestamp now we have all the info
    }

    private static async Task GetVendorInformationAndSettings(HttpClient client, ApiInformation apiInformation, VendorInformation vendorInformation) {
        apiInformation.ApiCallsVendors++;
        apiInformation.LastUpdated = DateTime.UtcNow;

        var url = $"accounts/vendors/{vendorInformation.VendorId}";
        Vendor response;

        try {
            // Use GetAsync instead of GetFromJsonAsync for more robust error handling
            var httpResponse = await client.GetAsync(requestUri: url);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting vendor. URL: {client.BaseAddress}{url}", vendor: vendorInformation.VendorId);
                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType}. URL: {client.BaseAddress}{url}", vendor: vendorInformation.VendorId);
                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Vendor>() ?? new Vendor();
        }
        catch (JsonException jsonEx) {
            StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting vendor. URL: {client.BaseAddress}{url}", vendor: vendorInformation.VendorId);
            StatsLogger.Log(stats: apiInformation, message: jsonEx.Message);
            return;
        }
        catch (TaskCanceledException tcEx) {
            StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation. URL: {client.BaseAddress}{url}", vendor: vendorInformation.VendorId);
            StatsLogger.Log(stats: apiInformation, message: tcEx.Message);
            return;
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error getting vendor. URL: {client.BaseAddress}{url}", vendor: vendorInformation.VendorId);
            StatsLogger.Log(stats: apiInformation, message: ex.Message);
            return;
        }

        var names = new List<string>();
        if (response.AvailableMapSets != null)
            foreach (var item in response.AvailableMapSets)
                if (item != null && item.Name != null)
                    names.Add(item: item.Name);

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
                    // StatsLogger.Log(apiInformation, System.Text.Json.JsonSerializer.Serialize(response.passwordPolicy), vendor: vendorInformation.VendorId);
                }

                vendorInformation.VendorPasswordPolicyPasswordExpirationDays = response.passwordPolicy.PasswordExpirationDays;

                if (response.passwordPolicy.OtpSettings != null) {
                    if (response.passwordPolicy.OtpSettings.Methods != null) {
                        if (response.passwordPolicy.OtpSettings.Methods.Totp != null) vendorInformation.VendorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Totp.TokenValidityDays;

                        if (response.passwordPolicy.OtpSettings.Methods.Email != null) vendorInformation.VendorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Email.TokenValidityDays;
                    }

                    vendorInformation.VendorPasswordPolicyOtpSettingsGracePeriodDays = response.passwordPolicy.OtpSettings.GracePeriodDays;
                    vendorInformation.VendorPasswordPolicyOtpSettingsMandatoryFor = response.passwordPolicy.OtpSettings.MandatoryFor;
                }
            }
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error processing vendor: {ex.Message}", vendor: vendorInformation.VendorId);
        }
    }


    public static async Task PopulateVendorInformation(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int counter = 0) {
        try {
            var pageSize = 10;
            StatsLogger.Log(stats: apiInformation, $"Fetching vendors < {reportDate:s}");
            var vendors = FetchUnprocessedVendors(db: db, pageSize: pageSize, reportDate: reportDate);
            StatsLogger.Log(stats: apiInformation, $"fetching {vendors.Count} vendors to update");

            if (vendors.Any()) {
                try {
                    await Parallel.ForEachAsync(source: vendors,
                        (vendor, cancellationToken) =>
                            GetVendorInformation(httpClient: httpClient, apiInformation: apiInformation, vendor: vendor, cancellationToken: cancellationToken));
                }
                catch (Exception ex) {
                    StatsLogger.Log(stats: apiInformation, $"Error during vendor information fetching: {ex.Message}");
                    // Continue with saving what we have
                }

                try {
                    await db.SaveChangesAsync();
                    counter += vendors.Count;
                    StatsLogger.Log(stats: apiInformation, $"checkpointed {vendors.Count} vendors");
                }
                catch (Exception ex) {
                    StatsLogger.Log(stats: apiInformation, $"Error saving vendor information to database: {ex.Message}");
                }

                // Process the next batch
                await PopulateVendorInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, counter: counter);
            }
            else {
                StatsLogger.Log(stats: apiInformation, $"PopulateVendorInformation complete {counter} prepared vendors");
            }
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Unhandled exception in PopulateVendorInformation: {ex.Message}");
            StatsLogger.Log(stats: apiInformation, message: ex.StackTrace);
        }
    }

    internal static List<VendorInformation> FetchAllProcessedVendors(StatsContext db, DateTime reportDate, ApiInformation? apiInformation) {
        try {
            var items = db.Vendors
                .Where(x => x.CreatedTimestamp > reportDate);
            return items.ToList();
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error fetching processed clients from database: {ex.Message}");
            return new List<VendorInformation>();
        }
    }
}