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
        DistributorInformation distributor, object o, CancellationToken cancellationToken) {
        var tasks = new List<Task> {
            GetDistributorInformationAndSettings(client: httpClient, apiInformation: apiInformation, distributorInformation: distributor)
            // GetDistributorStats(httpClient, apiInformation, distributor)
        };
        await Task.WhenAll(tasks: tasks);
        distributor.CreatedTimestamp = DateTime.UtcNow; // update the CreatedTimestamp now we have all the info
    }

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
                StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting distributor {distributorInformation.DistributorId}. URL: {client.BaseAddress}{url}");
                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType} for distributor {distributorInformation.DistributorId}. URL: {client.BaseAddress}{url}");
                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Distributor>() ?? new Distributor();
        }
        catch (JsonException jsonEx) {
            StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting distributor {distributorInformation.DistributorId}. URL: {client.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: jsonEx.Message);
            return;
        }
        catch (TaskCanceledException tcEx) {
            StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation getting distributor {distributorInformation.DistributorId}. URL: {client.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: tcEx.Message);
            return;
        }
        catch (Exception ex) {
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
        }
        catch (Exception ex) {
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
                            GetDistributorInformation(httpClient: httpClient, apiInformation: apiInformation, distributor: distributor, null, cancellationToken: cancellationToken));
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
}