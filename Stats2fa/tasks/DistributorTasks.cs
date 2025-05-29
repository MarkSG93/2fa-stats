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

namespace Stats2fa;

internal class DistributorTasks {
    private static List<DistributorInformation> FetchUnprocessedDistributors(StatsContext db, int pageSize,
        DateTime reportDate) {
        var pageIndex = 1;
        var distributors = db.Distributors
            .Where(x => x.CreatedTimestamp < reportDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(count: pageSize);
        return distributors.ToList();
    }

    private static async ValueTask GetDistributorInformation(HttpClient httpClient, ApiInformation apiInformation,
        DistributorInformation distributor, CancellationToken cancellationToken) {
        var tasks = new List<Task> {
            GetDistributorInformationAndSettings(client: httpClient, apiInformation: apiInformation, distributorInformation: distributor)
            // GetDistributorUsers(httpClient: httpClient, apiInformation: apiInformation, distributorInformation: distributor, cancellationToken: cancellationToken)
        };
        await Task.WhenAll(tasks: tasks);
        distributor.CreatedTimestamp = DateTime.UtcNow; // update the CreatedTimestamp now we have all the info
    }

    // private static async Task GetDistributorUsers(HttpClient httpClient, ApiInformation apiInformation, DistributorInformation distributorInformation, CancellationToken cancellationToken) {
    //     apiInformation.ApiCallsDistributors++;
    //     apiInformation.LastUpdated = DateTime.UtcNow;
    //
    //     var url = $"accounts/users?owner={distributorInformation.DistributorId}&offset=0&limit={10000}&sort=name:asc&filter=(state=inactive|state=active|state=suspended)";
    //     Users? response;
    //     try {
    //         // Use GetAsync with the cancellation token for proper cancellation support
    //         var httpResponse = await httpClient.GetAsync(requestUri: url, cancellationToken: cancellationToken);
    //
    //         // Check if the request was successful
    //         if (!httpResponse.IsSuccessStatusCode) {
    //             StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting distributor users. URL: {httpClient.BaseAddress}{url}", distributor: distributorInformation.DistributorId);
    //             return;
    //         }
    //
    //         // Check content type to ensure it's JSON
    //         var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
    //         if (contentType == null || !contentType.Contains("application/json")) {
    //             StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType} for distributor users. URL: {httpClient.BaseAddress}{url}", distributor: distributorInformation.DistributorId);
    //
    //             // For debugging: try to read the content as string to see what's being returned
    //             if (contentType?.Contains("text/html") == true)
    //                 try {
    //                     var htmlContent = await httpResponse.Content.ReadAsStringAsync();
    //                     var preview = htmlContent.Length > 100 ? htmlContent.Substring(0, 100) + "..." : htmlContent;
    //                     StatsLogger.Log(stats: apiInformation, $"HTML response preview: {preview}", distributor: distributorInformation.DistributorId);
    //
    //                     distributorInformation.DistributorStatsStatus = "ERROR_HTML_RESPONSE";
    //                 }
    //                 catch (Exception ex) {
    //                     StatsLogger.Log(stats: apiInformation, $"Error reading HTML content: {ex.Message}", distributor: distributorInformation.DistributorId);
    //                 }
    //
    //             return;
    //         }
    //
    //         // Read as JSON
    //         response = await httpResponse.Content.ReadFromJsonAsync<Users>(cancellationToken: cancellationToken);
    //     }
    //     catch (JsonException jsonEx) {
    //         StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting distributor users {distributorInformation.DistributorId}. URL: {httpClient.BaseAddress}{url}", distributor: distributorInformation.DistributorId);
    //         StatsLogger.Log(stats: apiInformation, message: jsonEx.Message);
    //         return;
    //     }
    //     catch (TaskCanceledException tcEx) {
    //         StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation getting distributor users {distributorInformation.DistributorId}. URL: {httpClient.BaseAddress}{url}", distributor: distributorInformation.DistributorId);
    //         StatsLogger.Log(stats: apiInformation, message: tcEx.Message);
    //         return;
    //     }
    //     catch (Exception ex) {
    //         StatsLogger.Log(stats: apiInformation, $"Error getting distributor users {distributorInformation.DistributorId}. URL: {httpClient.BaseAddress}{url}", distributor: distributorInformation.DistributorId);
    //         StatsLogger.Log(stats: apiInformation, message: ex.Message);
    //         return;
    //     }
    //
    //     // Safely set properties with null checks to avoid NullReferenceException
    //     try {
    //         if (response != null) distributorInformation.DistributorUsers = response;
    //
    //         distributorInformation.DistributorStatsStatus = "SUCCESS";
    //     }
    //     catch (Exception ex) {
    //         StatsLogger.Log(stats: apiInformation, $"Error processing user list for distributor {distributorInformation.DistributorId}: {ex.Message}", distributor: distributorInformation.DistributorId);
    //     }
    // }

    private static async Task GetDistributorInformationAndSettings(HttpClient client, ApiInformation apiInformation, DistributorInformation distributorInformation) {
        apiInformation.ApiCallsDistributors++;
        apiInformation.LastUpdated = DateTime.UtcNow;

        var url = $"accounts/distributors/{distributorInformation.DistributorId}";
        Distributor response;

        try {
            // Use GetAsync instead of GetFromJsonAsync for more robust error handling
            var httpResponse = await client.GetAsync(requestUri: url);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                distributorInformation.DistributorStatsStatus = "ERROR_HTTP_" + (int)httpResponse.StatusCode;
                StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting distributor {distributorInformation.DistributorId}. URL: {client.BaseAddress}{url}");
                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                distributorInformation.DistributorStatsStatus = "ERROR_CONTENT_TYPE";
                StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType} for distributor. URL: {client.BaseAddress}{url}", distributor: distributorInformation.DistributorId);
                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Distributor>() ?? new Distributor();
        }
        catch (JsonException jsonEx) {
            distributorInformation.DistributorStatsStatus = "ERROR_JSON_PARSING";
            StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting distributor {distributorInformation.DistributorId}. URL: {client.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: jsonEx.Message);
            return;
        }
        catch (TaskCanceledException tcEx) {
            distributorInformation.DistributorStatsStatus = "ERROR_TIMEOUT";
            StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation getting distributor {distributorInformation.DistributorId}. URL: {client.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: tcEx.Message);
            return;
        }
        catch (Exception ex) {
            distributorInformation.DistributorStatsStatus = "ERROR_GENERAL_EXCEPTION";
            StatsLogger.Log(stats: apiInformation, $"Error getting distributor {distributorInformation.DistributorId}. URL: {client.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: ex.Message);
            return;
        }

        // Safely set properties with null checks to avoid NullReferenceException
        try {
            if (response.passwordPolicy != null) {
                if (response.passwordPolicy.Source != null) {
                    distributorInformation.DistributorPasswordPolicySourceId = response.passwordPolicy.Source.Id;
                    distributorInformation.DistributorPasswordPolicySourceName = response.passwordPolicy.Source.Name;
                    distributorInformation.DistributorPasswordPolicySourceType = response.passwordPolicy.Source.Type;
                }

                distributorInformation.DistributorPasswordPolicyPasswordLength = response.passwordPolicy.PasswordLength;

                if (response.passwordPolicy.PasswordComplexity != null) {
                    distributorInformation.DistributorPasswordPolicyPasswordComplexityMixedcase = response.passwordPolicy.PasswordComplexity.MixedCase;
                    distributorInformation.DistributorPasswordPolicyPasswordComplexityAlphanumerical = response.passwordPolicy.PasswordComplexity.AlphaNumerical;
                    distributorInformation.DistributorPasswordPolicyPasswordComplexityNocommonpasswords = response.passwordPolicy.PasswordComplexity.NoCommonPasswords;
                    distributorInformation.DistributorPasswordPolicyPasswordComplexitySpecialcharacters = response.passwordPolicy.PasswordComplexity.SpecialCharacters;
                    StatsLogger.Log(stats: apiInformation, JsonSerializer.Serialize(value: response.passwordPolicy));
                }

                distributorInformation.DistributorPasswordPolicyPasswordExpirationDays = response.passwordPolicy.PasswordExpirationDays;

                if (response.passwordPolicy.OtpSettings != null) {
                    if (response.passwordPolicy.OtpSettings.Methods != null) {
                        if (response.passwordPolicy.OtpSettings.Methods.Totp != null) distributorInformation.DistributorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Totp.TokenValidityDays;

                        if (response.passwordPolicy.OtpSettings.Methods.Email != null) distributorInformation.DistributorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Email.TokenValidityDays;
                    }

                    distributorInformation.DistributorPasswordPolicyOtpSettingsGracePeriodDays = response.passwordPolicy.OtpSettings.GracePeriodDays;
                    distributorInformation.DistributorPasswordPolicyOtpSettingsMandatoryFor = response.passwordPolicy.OtpSettings.MandatoryFor;
                }
            }

            // Set the status to SUCCESS after processing
            distributorInformation.DistributorStatsStatus = "SUCCESS";
        }
        catch (Exception ex) {
            // Set status to ERROR_EXCEPTION in case of failure
            distributorInformation.DistributorStatsStatus = "ERROR_EXCEPTION";
            StatsLogger.Log(stats: apiInformation, $"Error processing distributor data for {distributorInformation.DistributorId}: {ex.Message}");
        }
    }

    public static async Task PopulateDistributorInformation(HttpClient httpClient, ApiInformation apiInformation,
        StatsContext db, DateTime reportDate, int counter = 0) {
        try {
            var pageSize = 5;
            StatsLogger.Log(stats: apiInformation, $"Fetching distributors < {reportDate:s}");
            var distributors = FetchUnprocessedDistributors(db: db, pageSize: pageSize, reportDate: reportDate);
            StatsLogger.Log(stats: apiInformation, $"fetching {distributors.Count} distributors to update");

            if (distributors.Any()) {
                try {
                    await Parallel.ForEachAsync(source: distributors,
                        (distributor, cancellationToken) =>
                            GetDistributorInformation(httpClient: httpClient, apiInformation: apiInformation, distributor: distributor, cancellationToken: cancellationToken));
                }
                catch (Exception ex) {
                    StatsLogger.Log(stats: apiInformation, $"Error during distributor information fetching: {ex.Message}");
                    // Continue with saving what we have
                }

                try {
                    await db.SaveChangesAsync();
                    counter += distributors.Count();
                    StatsLogger.Log(stats: apiInformation, $"checkpointed {distributors.Count()} distributors");
                }
                catch (Exception ex) {
                    StatsLogger.Log(stats: apiInformation, $"Error saving distributor information to database: {ex.Message}");
                }

                // Process the next batch
                await PopulateDistributorInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, counter: counter);
            }
            else {
                StatsLogger.Log(stats: apiInformation, $"PopulateDistributorInformation complete {counter} prepared distributors");
            }
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Unhandled exception in PopulateDistributorInformation: {ex.Message}");
            StatsLogger.Log(stats: apiInformation, message: ex.StackTrace);
        }
    }

    internal static List<DistributorInformation> FetchAllProcessedDistributors(StatsContext db, DateTime reportDate, ApiInformation? apiInformation) {
        try {
            var items = db.Distributors
                .Where(x => x.CreatedTimestamp > reportDate);
            return items.ToList();
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error fetching processed clients from database: {ex.Message}");
            return new List<DistributorInformation>();
        }
    }
}