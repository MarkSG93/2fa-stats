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
using Stats2fa.logger;
using Stats2fa.utils;

namespace Stats2fa.tasks;

internal class ClientTasks {
    // Add recursive depth tracking to prevent infinite recursion
    public static async Task PopulateClientInformation(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int maxClients = 0, int counter = 0, int recursionDepth = 0) {
        // Maximum recursion depth to prevent stack overflow
        const int MaxRecursionDepth = 100;
        if (recursionDepth > MaxRecursionDepth) {
            StatsLogger.Log(apiInformation, $"Maximum recursion depth reached ({MaxRecursionDepth}). Stopping client population to prevent infinite loop.");
            return;
        }

        // Maximum clients check
        var pageSize = 100;
        if (maxClients > 0 && counter >= maxClients) {
            StatsLogger.Log(apiInformation, $"Reached maximum specified clients ({maxClients}). Stopping further processing.");
            return;
        }

        // Process unprocessed clients
        try {
            StatsLogger.Log(apiInformation, $"Fetching clients < {reportDate:s} (Batch {recursionDepth + 1})");
            List<ClientInformation> clients = FetchUnprocessedClients(db, pageSize, reportDate, apiInformation);
            StatsLogger.Log(apiInformation, $"Fetching {clients.Count} clients to update");

            if (clients.Any()) {
                int processedCount = 0;
                try {
                    // Limit parallelism to avoid overwhelming the API and causing timeouts
                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(5, Environment.ProcessorCount) };

                    await Parallel.ForEachAsync(source: clients, parallelOptions, async (client, cancellationToken) =>
                    {
                        try {
                            await GetClientInformation(httpClient, apiInformation, client, cancellationToken);
                            Interlocked.Increment(ref processedCount);

                            // Log progress periodically
                            int current = Interlocked.CompareExchange(ref processedCount, 0, 0);
                            if (current % 10 == 0) {
                                StatsLogger.Log(apiInformation, $"Progress: Processed {current}/{clients.Count} clients");
                            }
                        }
                        catch (Exception ex) {
                            StatsLogger.Log(apiInformation, $"Error processing client {client.ClientId}: {ex.Message}");
                            // Continue with other clients despite this error
                        }
                    });

                    StatsLogger.Log(apiInformation, $"Completed processing {processedCount}/{clients.Count} clients in batch {recursionDepth + 1}");

                    try {
                        StatsLogger.Log(apiInformation, "Saving changes to database...");
                        await db.SaveChangesAsync();
                        counter += clients.Count;
                        StatsLogger.Log(apiInformation, $"Checkpointed {clients.Count} clients");
                    }
                    catch (Exception ex) {
                        StatsLogger.Log(apiInformation, $"Error saving client data to database: {ex.Message}");
                    }

                    // Continue with next batch with increment of recursion depth
                    await PopulateClientInformation(httpClient, apiInformation, db, reportDate, maxClients, counter, recursionDepth + 1);
                }
                catch (Exception ex) {
                    StatsLogger.Log(apiInformation, $"Error during parallel client processing: {ex.Message}");
                    // Try to continue with the next batch
                    counter += clients.Count;
                    await PopulateClientInformation(httpClient, apiInformation, db, reportDate, maxClients, counter, recursionDepth + 1);
                }
            }
            else {
                StatsLogger.Log(apiInformation, $"No more unprocessed clients found. {counter} clients prepared.");

                // Only process corrupted clients after we're done with unprocessed clients
                await ProcessCorruptedClients(httpClient, apiInformation, db, reportDate, maxClients, counter);
            }
        }
        catch (Exception ex) {
            StatsLogger.Log(apiInformation, $"Error fetching unprocessed clients: {ex.Message}");
            // Try to process corrupted clients even if unprocessed clients failed
            await ProcessCorruptedClients(httpClient, apiInformation, db, reportDate, maxClients, counter);
        }
    }

    // Separate method for corrupted clients to improve code structure
    private static async Task ProcessCorruptedClients(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int maxClients, int counter, int recursionDepth = 0) {
        const int MaxRecursionDepth = 100;
        if (recursionDepth > MaxRecursionDepth) {
            StatsLogger.Log(apiInformation, $"Maximum recursion depth reached for corrupted clients ({MaxRecursionDepth}). Stopping to prevent infinite loop.");
            return;
        }

        var pageSize = 100;

        try {
            StatsLogger.Log(apiInformation, $"Fetching corrupted clients (Batch {recursionDepth + 1})");
            List<ClientInformation> clients = FetchCorruptedClients(db, pageSize, reportDate, apiInformation);
            StatsLogger.Log(apiInformation, $"Fetched {clients.Count} corrupted clients to retry");

            if (clients.Any()) {
                int processedCount = 0;
                try {
                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(5, Environment.ProcessorCount) };

                    await Parallel.ForEachAsync(source: clients, parallelOptions, async (client, cancellationToken) =>
                    {
                        try {
                            await GetClientInformation(httpClient, apiInformation, client, cancellationToken);
                            Interlocked.Increment(ref processedCount);

                            int current = Interlocked.CompareExchange(ref processedCount, 0, 0);
                            if (current % 10 == 0) {
                                StatsLogger.Log(apiInformation, $"Progress: Retried {current}/{clients.Count} corrupted clients");
                            }
                        }
                        catch (Exception ex) {
                            StatsLogger.Log(apiInformation, $"Error retrying corrupted client {client.ClientId}: {ex.Message}");
                        }
                    });

                    StatsLogger.Log(apiInformation, $"Completed retrying {processedCount}/{clients.Count} corrupted clients in batch {recursionDepth + 1}");

                    try {
                        StatsLogger.Log(apiInformation, "Saving changes to database...");
                        await db.SaveChangesAsync();
                        counter += clients.Count;
                        StatsLogger.Log(apiInformation, $"Checkpointed {clients.Count} corrupted clients");
                    }
                    catch (Exception ex) {
                        StatsLogger.Log(apiInformation, $"Error saving corrupted client data to database: {ex.Message}");
                    }

                    // Continue with next batch
                    await ProcessCorruptedClients(httpClient, apiInformation, db, reportDate, maxClients, counter, recursionDepth + 1);
                }
                catch (Exception ex) {
                    StatsLogger.Log(apiInformation, $"Error during parallel corrupted client processing: {ex.Message}");
                    await ProcessCorruptedClients(httpClient, apiInformation, db, reportDate, maxClients, counter, recursionDepth + 1);
                }
            }
            else {
                StatsLogger.Log(apiInformation, $"No more corrupted clients to retry. All client processing complete.");
            }
        }
        catch (Exception ex) {
            StatsLogger.Log(apiInformation, $"Error fetching corrupted clients: {ex.Message}");
        }
    }

    private static async ValueTask GetClientInformation(HttpClient httpClient, ApiInformation apiInformation, ClientInformation client, CancellationToken cancellationToken) {
        try {
            // Create a new linked cancellation token source that combines our token with a timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Set a timeout of 60 seconds for each client operation
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

            var tasks = new List<Task> { GetClientInformationAndSettings(httpClient, apiInformation, client, timeoutCts.Token) };

            await Task.WhenAll(tasks);
            client.CreatedTimestamp = DateTime.UtcNow;
        }
        catch (OperationCanceledException) {
            // Handle cancellation gracefully
            StatsLogger.Log(apiInformation, $"Operation for client {client.ClientId} was cancelled");
            client.ClientStatsStatus = "ERROR_TIMEOUT";
            throw;
        }
        catch (Exception ex) {
            StatsLogger.Log(apiInformation, $"Error in GetClientInformation for client {client.ClientId}: {ex.Message}");
            client.ClientStatsStatus = "ERROR_EXCEPTION";
            throw;
        }
    }

    private static async Task GetClientInformationAndSettings(HttpClient httpClient, ApiInformation apiInformation, ClientInformation clientInformation, CancellationToken cancellationToken = default) {
        apiInformation.ApiCallsClients++;
        apiInformation.LastUpdated = DateTime.UtcNow;

        string url = $"accounts/clients/{clientInformation.ClientId}";
        Client response;

        try {
            // Use GetAsync with the cancellation token for proper cancellation support
            var httpResponse = await httpClient.GetAsync(url, cancellationToken);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                StatsLogger.Log(apiInformation, $"HTTP error {httpResponse.StatusCode} getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");
                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                StatsLogger.Log(apiInformation, $"Unexpected content type: {contentType} for client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");

                // For debugging: try to read the content as string to see what's being returned
                if (contentType?.Contains("text/html") == true) {
                    try {
                        string htmlContent = await httpResponse.Content.ReadAsStringAsync();
                        // string preview = htmlContent.Length > 100 ? htmlContent.Substring(0, 100) + "..." : htmlContent;
                        StatsLogger.Log(apiInformation, $"HTML response preview: {htmlContent}");

                        clientInformation.ClientStatsStatus = "ERROR_HTML_RESPONSE";
                    }
                    catch (Exception ex) {
                        StatsLogger.Log(apiInformation, $"Error reading HTML content: {ex.Message}");
                    }
                }

                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Client>() ?? new Client();
        }
        catch (System.Text.Json.JsonException jsonEx) {
            StatsLogger.Log(apiInformation, $"JSON parsing error getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(apiInformation, jsonEx.Message);
            return;
        }
        catch (TaskCanceledException tcEx) {
            StatsLogger.Log(apiInformation, $"Request timeout or cancellation getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(apiInformation, tcEx.Message);
            return;
        }
        catch (Exception ex) {
            StatsLogger.Log(apiInformation, $"Error getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(apiInformation, ex.Message);
            return;
        }

        // Safely set properties with null checks to avoid NullReferenceException
        try {
            if (response.passwordPolicy != null) {
                if (response.passwordPolicy.Source != null) {
                    clientInformation.ClientPasswordPolicySourceId = response.passwordPolicy.Source.Id;
                    clientInformation.ClientPasswordPolicySourceName = response.passwordPolicy.Source.Name;
                    clientInformation.ClientPasswordPolicySourceType = response.passwordPolicy.Source.Type;
                }

                clientInformation.ClientPasswordPolicyPasswordLength = response.passwordPolicy.PasswordLength;

                if (response.passwordPolicy.PasswordComplexity != null) {
                    clientInformation.ClientPasswordPolicyPasswordComplexityMixedcase = response.passwordPolicy.PasswordComplexity.MixedCase;
                    clientInformation.ClientPasswordPolicyPasswordComplexityAlphanumerical = response.passwordPolicy.PasswordComplexity.AlphaNumerical;
                    clientInformation.ClientPasswordPolicyPasswordComplexityNocommonpasswords = response.passwordPolicy.PasswordComplexity.NoCommonPasswords;
                    clientInformation.ClientPasswordPolicyPasswordComplexitySpecialcharacters = response.passwordPolicy.PasswordComplexity.SpecialCharacters;
                    // Uncomment for debugging
                    // StatsLogger.Log(apiInformation,System.Text.Json.JsonSerializer.Serialize(response.passwordPolicy));
                }

                clientInformation.ClientPasswordPolicyPasswordExpirationDays = response.passwordPolicy.PasswordExpirationDays;

                if (response.passwordPolicy.OtpSettings != null) {
                    if (response.passwordPolicy.OtpSettings.Methods != null) {
                        if (response.passwordPolicy.OtpSettings.Methods.Totp != null) {
                            clientInformation.ClientPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Totp.TokenValidityDays;
                        }

                        if (response.passwordPolicy.OtpSettings.Methods.Email != null) {
                            clientInformation.ClientPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Email.TokenValidityDays;
                        }
                    }

                    clientInformation.ClientPasswordPolicyOtpSettingsGracePeriodDays = response.passwordPolicy.OtpSettings.GracePeriodDays;
                    clientInformation.ClientPasswordPolicyOtpSettingsMandatoryFor = response.passwordPolicy.OtpSettings.MandatoryFor;
                }
            }

            clientInformation.ClientStatsStatus = "SUCCESS";
        }
        catch (Exception ex) {
            StatsLogger.Log(apiInformation, $"Error processing client data for {clientInformation.ClientId}: {ex.Message}");
        }
    }

    internal static List<ClientInformation> FetchCorruptedClients(StatsContext db, int pageSize, DateTime reportDate, ApiInformation? apiInformation) {
        try {
            var pageIndex = 1;
            var clients = db.Clients
                .Where(x => x.ClientStatsStatus == "ERROR_HTML_RESPONSE")
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);
            return clients.ToList();
        }
        catch (Exception ex) {
            StatsLogger.Log(apiInformation, $"Error fetching corrupted clients from database: {ex.Message}");
            return new List<ClientInformation>();
        }
    }

    internal static List<ClientInformation> FetchUnprocessedClients(StatsContext db, int pageSize, DateTime reportDate, ApiInformation? apiInformation) {
        try {
            var pageIndex = 1;
            var clients = db.Clients
                .Where(x => x.CreatedTimestamp < reportDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);
            return clients.ToList();
        }
        catch (Exception ex) {
            StatsLogger.Log(apiInformation, $"Error fetching unprocessed clients from database: {ex.Message}");
            return new List<ClientInformation>();
        }
    }

    internal static List<ClientInformation> FetchAllProcessedClients(StatsContext db, DateTime reportDate, ApiInformation? apiInformation) {
        try {
            var clients = db.Clients
                .Where(x => x.CreatedTimestamp > reportDate);
            return clients.ToList();
        }
        catch (Exception ex) {
            StatsLogger.Log(apiInformation, $"Error fetching processed clients from database: {ex.Message}");
            return new List<ClientInformation>();
        }
    }
}