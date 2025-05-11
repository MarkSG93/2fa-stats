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

internal class ClientTasks {
    // Add recursive depth tracking to prevent infinite recursion
    public static async Task PopulateClientInformation(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int maxClients = 0, int counter = 0, int recursionDepth = 0) {
        // Maximum recursion depth to prevent stack overflow
        const int MaxRecursionDepth = 100;
        if (recursionDepth > MaxRecursionDepth) {
            StatsLogger.Log(stats: apiInformation, $"Maximum recursion depth reached ({MaxRecursionDepth}). Stopping client population to prevent infinite loop.");
            return;
        }

        // Maximum clients check
        var pageSize = 100;
        if (maxClients > 0 && counter >= maxClients) {
            StatsLogger.Log(stats: apiInformation, $"Reached maximum specified clients ({maxClients}). Stopping further processing.");
            return;
        }

        // Process unprocessed clients
        try {
            StatsLogger.Log(stats: apiInformation, $"Fetching clients < {reportDate:s} (Batch {recursionDepth + 1})");
            var clients = FetchUnprocessedClients(db: db, pageSize: pageSize, reportDate: reportDate, apiInformation: apiInformation);
            StatsLogger.Log(stats: apiInformation, $"Fetching {clients.Count} clients to update");

            if (clients.Any()) {
                var processedCount = 0;
                try {
                    // Limit parallelism to avoid overwhelming the API and causing timeouts
                    var parallelOptions = new ParallelOptions {
                        MaxDegreeOfParallelism = Math.Min(5, val2: Environment.ProcessorCount)
                    };

                    await Parallel.ForEachAsync(source: clients,
                        parallelOptions: parallelOptions,
                        async (client, cancellationToken) =>
                        {
                            try {
                                await GetClientInformation(httpClient: httpClient, apiInformation: apiInformation, client: client, cancellationToken: cancellationToken);
                                Interlocked.Increment(location: ref processedCount);

                                // Log progress periodically
                                var current = Interlocked.CompareExchange(location1: ref processedCount, 0, 0);
                                if (current % 10 == 0) StatsLogger.Log(stats: apiInformation, $"Progress: Processed {current}/{clients.Count} clients");
                            }
                            catch (Exception ex) {
                                StatsLogger.Log(stats: apiInformation, $"Error processing client {client.ClientId}: {ex.Message}");
                                // Continue with other clients despite this error
                            }
                        });

                    StatsLogger.Log(stats: apiInformation, $"Completed processing {processedCount}/{clients.Count} clients in batch {recursionDepth + 1}");

                    try {
                        StatsLogger.Log(stats: apiInformation, "Saving changes to database...");
                        await db.SaveChangesAsync();
                        counter += clients.Count;
                        StatsLogger.Log(stats: apiInformation, $"Checkpointed {clients.Count} clients");
                    }
                    catch (Exception ex) {
                        StatsLogger.Log(stats: apiInformation, $"Error saving client data to database: {ex.Message}");
                    }

                    // Continue with next batch with increment of recursion depth
                    await PopulateClientInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, maxClients: maxClients, counter: counter, recursionDepth + 1);
                }
                catch (Exception ex) {
                    StatsLogger.Log(stats: apiInformation, $"Error during parallel client processing: {ex.Message}");
                    // Try to continue with the next batch
                    counter += clients.Count;
                    await PopulateClientInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, maxClients: maxClients, counter: counter, recursionDepth + 1);
                }
            }
            else {
                StatsLogger.Log(stats: apiInformation, $"No more unprocessed clients found. {counter} clients prepared.");

                // Only process corrupted clients after we're done with unprocessed clients
                await ProcessCorruptedClients(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, maxClients: maxClients, counter: counter);
            }
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error fetching unprocessed clients: {ex.Message}");
            // Try to process corrupted clients even if unprocessed clients failed
            await ProcessCorruptedClients(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, maxClients: maxClients, counter: counter);
        }
    }

    // Separate method for corrupted clients to improve code structure
    private static async Task ProcessCorruptedClients(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int maxClients, int counter, int recursionDepth = 0) {
        const int MaxRecursionDepth = 100;
        if (recursionDepth > MaxRecursionDepth) {
            StatsLogger.Log(stats: apiInformation, $"Maximum recursion depth reached for corrupted clients ({MaxRecursionDepth}). Stopping to prevent infinite loop.");
            return;
        }

        var pageSize = 100;

        try {
            StatsLogger.Log(stats: apiInformation, $"Fetching corrupted clients (Batch {recursionDepth + 1})");
            var clients = FetchCorruptedClients(db: db, pageSize: pageSize, reportDate: reportDate, apiInformation: apiInformation);
            StatsLogger.Log(stats: apiInformation, $"Fetched {clients.Count} corrupted clients to retry");

            if (clients.Any()) {
                var processedCount = 0;
                try {
                    var parallelOptions = new ParallelOptions {
                        MaxDegreeOfParallelism = Math.Min(5, val2: Environment.ProcessorCount)
                    };

                    await Parallel.ForEachAsync(source: clients,
                        parallelOptions: parallelOptions,
                        async (client, cancellationToken) =>
                        {
                            try {
                                await GetClientInformation(httpClient: httpClient, apiInformation: apiInformation, client: client, cancellationToken: cancellationToken);
                                Interlocked.Increment(location: ref processedCount);

                                var current = Interlocked.CompareExchange(location1: ref processedCount, 0, 0);
                                if (current % 10 == 0) StatsLogger.Log(stats: apiInformation, $"Progress: Retried {current}/{clients.Count} corrupted clients");
                            }
                            catch (Exception ex) {
                                StatsLogger.Log(stats: apiInformation, $"Error retrying corrupted client: {ex.Message}", client: client.ClientId);
                            }
                        });

                    StatsLogger.Log(stats: apiInformation, $"Completed retrying {processedCount}/{clients.Count} corrupted clients in batch {recursionDepth + 1}");

                    try {
                        StatsLogger.Log(stats: apiInformation, "Saving changes to database...");
                        await db.SaveChangesAsync();
                        counter += clients.Count;
                        StatsLogger.Log(stats: apiInformation, $"Checkpointed {clients.Count} corrupted clients");
                    }
                    catch (Exception ex) {
                        StatsLogger.Log(stats: apiInformation, $"Error saving corrupted client data to database: {ex.Message}");
                    }

                    // Continue with next batch
                    await ProcessCorruptedClients(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, maxClients: maxClients, counter: counter, recursionDepth + 1);
                }
                catch (Exception ex) {
                    StatsLogger.Log(stats: apiInformation, $"Error during parallel corrupted client processing: {ex.Message}");
                    await ProcessCorruptedClients(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, maxClients: maxClients, counter: counter, recursionDepth + 1);
                }
            }
            else {
                StatsLogger.Log(stats: apiInformation, "No more corrupted clients to retry. All client processing complete.");
            }
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error fetching corrupted clients: {ex.Message}");
        }
    }

    private static async ValueTask GetClientInformation(HttpClient httpClient, ApiInformation apiInformation, ClientInformation client, CancellationToken cancellationToken) {
        try {
            // Create a new linked cancellation token source that combines our token with a timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token: cancellationToken);
            // Set a timeout of 60 seconds for each client operation
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

            var tasks = new List<Task> {
                GetClientInformationAndSettings(httpClient: httpClient, apiInformation: apiInformation, clientInformation: client, cancellationToken: timeoutCts.Token),
                GetClientUsers(httpClient: httpClient, apiInformation: apiInformation, clientInformation: client, cancellationToken: timeoutCts.Token)
            };

            await Task.WhenAll(tasks: tasks);
            client.CreatedTimestamp = DateTime.UtcNow;
        }
        catch (OperationCanceledException) {
            // Handle cancellation gracefully
            StatsLogger.Log(stats: apiInformation, "Operation for client was cancelled", client: client.ClientId);
            client.ClientStatsStatus = "ERROR_TIMEOUT";
            throw;
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error in GetClientInformation for client: {ex.Message}", client: client.ClientId);
            client.ClientStatsStatus = "ERROR_EXCEPTION";
            throw;
        }
    }

    private static async Task GetClientUsers(HttpClient httpClient, ApiInformation apiInformation, ClientInformation clientInformation, UserInformation userInformation, CancellationToken cancellationToken) {
        apiInformation.ApiCallsClients++;
        apiInformation.LastUpdated = DateTime.UtcNow;

        var url = $"accounts/users?owner={clientInformation.ClientId}&offset=0&limit={10000}&sort=name:asc&filter=(state=inactive|state=active|state=suspended)";
        Users? response;
        try {
            // Use GetAsync with the cancellation token for proper cancellation support
            var httpResponse = await httpClient.GetAsync(requestUri: url, cancellationToken: cancellationToken);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting client. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);
                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType} for client. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);

                // For debugging: try to read the content as string to see what's being returned
                if (contentType?.Contains("text/html") == true)
                    try {
                        var htmlContent = await httpResponse.Content.ReadAsStringAsync();
                        var preview = htmlContent.Length > 100 ? htmlContent.Substring(0, 100) + "..." : htmlContent;
                        StatsLogger.Log(stats: apiInformation, $"HTML response preview: {preview}", client: clientInformation.ClientId);

                        clientInformation.ClientStatsStatus = "ERROR_HTML_RESPONSE";
                    }
                    catch (Exception ex) {
                        StatsLogger.Log(stats: apiInformation, $"Error reading HTML content: {ex.Message}", client: clientInformation.ClientId);
                    }

                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Users>(cancellationToken: cancellationToken);
        }
        catch (JsonException jsonEx) {
            StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);
            StatsLogger.Log(stats: apiInformation, message: jsonEx.Message);
            return;
        }
        catch (TaskCanceledException tcEx) {
            StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);
            StatsLogger.Log(stats: apiInformation, message: tcEx.Message);
            return;
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);
            StatsLogger.Log(stats: apiInformation, message: ex.Message);
            return;
        }

        // Safely set properties with null checks to avoid NullReferenceException
        try {
            // TODO save the response to the UserInformation table
            SaveUsers(userInformation, response);
            clientInformation.ClientStatsStatus = "SUCCESS";
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error processing user list for {clientInformation.ClientId}: {ex.Message}", client: clientInformation.ClientId);
        }
    }

    private static void SaveUsers(UserInformation userInformation, Users? response) {
       if (response == null) return;
       if (!response.UserList.Any())return;
       foreach (var user in response.UserList) {
           userInformation.UserId = user.Id;
           // TODO save all the other information to the userInformation
       }
        
    }

    private static async Task GetClientInformationAndSettings(HttpClient httpClient, ApiInformation apiInformation, ClientInformation clientInformation, CancellationToken cancellationToken = default) {
        apiInformation.ApiCallsClients++;
        apiInformation.LastUpdated = DateTime.UtcNow;

        var url = $"accounts/clients/{clientInformation.ClientId}";
        Client response;

        try {
            // Use GetAsync with the cancellation token for proper cancellation support
            var httpResponse = await httpClient.GetAsync(requestUri: url, cancellationToken: cancellationToken);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);
                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType} for client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);

                // For debugging: try to read the content as string to see what's being returned
                if (contentType?.Contains("text/html") == true)
                    try {
                        var htmlContent = await httpResponse.Content.ReadAsStringAsync();
                        var preview = htmlContent.Length > 100 ? htmlContent.Substring(0, 100) + "..." : htmlContent;
                        StatsLogger.Log(stats: apiInformation, $"HTML response preview: {preview}", client: clientInformation.ClientId);

                        clientInformation.ClientStatsStatus = "ERROR_HTML_RESPONSE";
                    }
                    catch (Exception ex) {
                        StatsLogger.Log(stats: apiInformation, $"Error reading HTML content: {ex.Message}", client: clientInformation.ClientId);
                    }

                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Client>() ?? new Client();
        }
        catch (JsonException jsonEx) {
            StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting client. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);
            StatsLogger.Log(stats: apiInformation, message: jsonEx.Message, client: clientInformation.ClientId);
            return;
        }
        catch (TaskCanceledException tcEx) {
            StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation getting client. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);
            StatsLogger.Log(stats: apiInformation, message: tcEx.Message, client: clientInformation.ClientId);
            return;
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error getting client. URL: {httpClient.BaseAddress}{url}", client: clientInformation.ClientId);
            StatsLogger.Log(stats: apiInformation, message: ex.Message, client: clientInformation.ClientId);
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
                        if (response.passwordPolicy.OtpSettings.Methods.Totp != null) clientInformation.ClientPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Totp.TokenValidityDays;

                        if (response.passwordPolicy.OtpSettings.Methods.Email != null) clientInformation.ClientPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Email.TokenValidityDays;
                    }

                    clientInformation.ClientPasswordPolicyOtpSettingsGracePeriodDays = response.passwordPolicy.OtpSettings.GracePeriodDays;
                    clientInformation.ClientPasswordPolicyOtpSettingsMandatoryFor = response.passwordPolicy.OtpSettings.MandatoryFor;
                }
            }

            clientInformation.ClientStatsStatus = "SUCCESS";
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error processing client data for {clientInformation.ClientId}: {ex.Message}", client: clientInformation.ClientId);
        }
    }

    internal static List<ClientInformation> FetchCorruptedClients(StatsContext db, int pageSize, DateTime reportDate, ApiInformation? apiInformation) {
        try {
            var pageIndex = 1;
            var clients = db.Clients
                .Where(x => x.ClientStatsStatus == "ERROR_HTML_RESPONSE")
                .Skip((pageIndex - 1) * pageSize)
                .Take(count: pageSize);
            return clients.ToList();
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error fetching corrupted clients from database: {ex.Message}");
            return new List<ClientInformation>();
        }
    }

    internal static List<ClientInformation> FetchUnprocessedClients(StatsContext db, int pageSize, DateTime reportDate, ApiInformation? apiInformation) {
        try {
            var pageIndex = 1;
            var clients = db.Clients
                .Where(x => x.CreatedTimestamp < reportDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(count: pageSize);
            return clients.ToList();
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error fetching unprocessed clients from database: {ex.Message}");
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
            StatsLogger.Log(stats: apiInformation, $"Error fetching processed clients from database: {ex.Message}");
            return new List<ClientInformation>();
        }
    }
}