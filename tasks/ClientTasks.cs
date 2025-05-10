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

internal class ClientTasks {
    // Add recursive depth tracking to prevent infinite recursion
    public static async Task PopulateClientInformation(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int maxClients = 0, int counter = 0, int recursionDepth = 0) {
        // Maximum recursion depth to prevent stack overflow
        const int MaxRecursionDepth = 100;
        if (recursionDepth > MaxRecursionDepth) {
            Console.WriteLine($"Maximum recursion depth reached ({MaxRecursionDepth}). Stopping client population to prevent infinite loop.");
            return;
        }

        // Maximum clients check
        var pageSize = 100;
        if (maxClients > 0 && counter >= maxClients) {
            Console.WriteLine($"Reached maximum specified clients ({maxClients}). Stopping further processing.");
            return;
        }

        // Process unprocessed clients
        try {
            Console.WriteLine($"Fetching clients < {reportDate:s} (Batch {recursionDepth + 1})");
            List<ClientInformation> clients = FetchUnprocessedClients(db, pageSize, reportDate);
            Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"fetching {clients.Count} clients to update"));

            if (clients.Any()) {
                int processedCount = 0;
                try {
                    // Limit parallelism to avoid overwhelming the API and causing timeouts
                    var parallelOptions = new ParallelOptions {
                        MaxDegreeOfParallelism = Math.Min(5, Environment.ProcessorCount)
                    };

                    await Parallel.ForEachAsync(source: clients, parallelOptions, async (client, cancellationToken) =>
                    {
                        try {
                            await GetClientInformation(httpClient, apiInformation, client, cancellationToken);
                            Interlocked.Increment(ref processedCount);

                            // Log progress periodically
                            int current = Interlocked.CompareExchange(ref processedCount, 0, 0);
                            if (current % 10 == 0) {
                                Console.WriteLine($"Progress: Processed {current}/{clients.Count} clients");
                            }
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Error processing client {client.ClientId}: {ex.Message}");
                            // Continue with other clients despite this error
                        }
                    });

                    Console.WriteLine($"Completed processing {processedCount}/{clients.Count} clients in batch {recursionDepth + 1}");

                    try {
                        Console.WriteLine("Saving changes to database...");
                        await db.SaveChangesAsync();
                        counter += clients.Count;
                        Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"checkpointed {clients.Count} clients"));
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Error saving client data to database: {ex.Message}");
                    }

                    // Continue with next batch with increment of recursion depth
                    await PopulateClientInformation(httpClient, apiInformation, db, reportDate, maxClients, counter, recursionDepth + 1);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error during parallel client processing: {ex.Message}");
                    // Try to continue with the next batch
                    counter += clients.Count;
                    await PopulateClientInformation(httpClient, apiInformation, db, reportDate, maxClients, counter, recursionDepth + 1);
                }
            }
            else {
                Console.WriteLine(StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"No more unprocessed clients found. {counter} clients prepared."));

                // Only process corrupted clients after we're done with unprocessed clients
                await ProcessCorruptedClients(httpClient, apiInformation, db, reportDate, maxClients, counter);
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error fetching unprocessed clients: {ex.Message}");
            // Try to process corrupted clients even if unprocessed clients failed
            await ProcessCorruptedClients(httpClient, apiInformation, db, reportDate, maxClients, counter);
        }
    }

    // Separate method for corrupted clients to improve code structure
    private static async Task ProcessCorruptedClients(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int maxClients, int counter, int recursionDepth = 0) {
        const int MaxRecursionDepth = 100;
        if (recursionDepth > MaxRecursionDepth) {
            Console.WriteLine($"Maximum recursion depth reached for corrupted clients ({MaxRecursionDepth}). Stopping to prevent infinite loop.");
            return;
        }

        var pageSize = 100;

        try {
            Console.WriteLine($"Fetching corrupted clients (Batch {recursionDepth + 1})");
            List<ClientInformation> clients = FetchCorruptedClients(db, pageSize, reportDate);
            Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"fetched {clients.Count} corrupted clients to retry"));

            if (clients.Any()) {
                int processedCount = 0;
                try {
                    var parallelOptions = new ParallelOptions {
                        MaxDegreeOfParallelism = Math.Min(5, Environment.ProcessorCount)
                    };

                    await Parallel.ForEachAsync(source: clients, parallelOptions, async (client, cancellationToken) =>
                    {
                        try {
                            await GetClientInformation(httpClient, apiInformation, client, cancellationToken);
                            Interlocked.Increment(ref processedCount);

                            int current = Interlocked.CompareExchange(ref processedCount, 0, 0);
                            if (current % 10 == 0) {
                                Console.WriteLine($"Progress: Retried {current}/{clients.Count} corrupted clients");
                            }
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Error retrying corrupted client {client.ClientId}: {ex.Message}");
                        }
                    });

                    Console.WriteLine($"Completed retrying {processedCount}/{clients.Count} corrupted clients in batch {recursionDepth + 1}");

                    try {
                        Console.WriteLine("Saving changes to database...");
                        await db.SaveChangesAsync();
                        counter += clients.Count;
                        Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"checkpointed {clients.Count} corrupted clients"));
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Error saving corrupted client data to database: {ex.Message}");
                    }

                    // Continue with next batch
                    await ProcessCorruptedClients(httpClient, apiInformation, db, reportDate, maxClients, counter, recursionDepth + 1);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error during parallel corrupted client processing: {ex.Message}");
                    await ProcessCorruptedClients(httpClient, apiInformation, db, reportDate, maxClients, counter, recursionDepth + 1);
                }
            }
            else {
                Console.WriteLine(StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"No more corrupted clients to retry. All client processing complete."));
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error fetching corrupted clients: {ex.Message}");
        }
    }

    private static async ValueTask GetClientInformation(HttpClient httpClient, ApiInformation apiInformation, ClientInformation client, CancellationToken cancellationToken) {
        try {
            // Create a new linked cancellation token source that combines our token with a timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // Set a timeout of 60 seconds for each client operation
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

            var tasks = new List<Task> {
                GetClientInformationAndSettings(httpClient, apiInformation, client, timeoutCts.Token)
            };

            await Task.WhenAll(tasks);
            client.CreatedTimestamp = DateTime.UtcNow;
        }
        catch (OperationCanceledException) {
            // Handle cancellation gracefully
            Console.WriteLine($"Operation for client {client.ClientId} was cancelled");
            client.ClientStatsStatus = "ERROR_TIMEOUT";
            throw;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error in GetClientInformation for client {client.ClientId}: {ex.Message}");
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
                Console.WriteLine($"HTTP error {httpResponse.StatusCode} getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");
                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                Console.WriteLine($"Unexpected content type: {contentType} for client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");

                // For debugging: try to read the content as string to see what's being returned
                if (contentType?.Contains("text/html") == true) {
                    try {
                        string htmlContent = await httpResponse.Content.ReadAsStringAsync();
                        // string preview = htmlContent.Length > 100 ? htmlContent.Substring(0, 100) + "..." : htmlContent;
                        Console.WriteLine($"HTML response preview: {htmlContent}");

                        clientInformation.ClientStatsStatus = "ERROR_HTML_RESPONSE";
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Error reading HTML content: {ex.Message}");
                    }
                }

                return;
            }

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<Client>() ?? new Client();
        }
        catch (System.Text.Json.JsonException jsonEx) {
            Console.WriteLine($"JSON parsing error getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");
            Console.WriteLine(jsonEx.Message);
            return;
        }
        catch (TaskCanceledException tcEx) {
            Console.WriteLine($"Request timeout or cancellation getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");
            Console.WriteLine(tcEx.Message);
            return;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error getting client {clientInformation.ClientId}. URL: {httpClient.BaseAddress}{url}");
            Console.WriteLine(ex.Message);
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
                    // Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(response.passwordPolicy));
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
            Console.WriteLine($"Error processing client data for {clientInformation.ClientId}: {ex.Message}");
        }
    }

    internal static List<ClientInformation> FetchCorruptedClients(StatsContext db, int pageSize, DateTime reportDate) {
        try {
            var pageIndex = 1;
            var clients = db.Clients
                .Where(x => x.ClientStatsStatus == "ERROR_HTML_RESPONSE")
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);
            return clients.ToList();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error fetching corrupted clients from database: {ex.Message}");
            return new List<ClientInformation>();
        }
    }

    internal static List<ClientInformation> FetchUnprocessedClients(StatsContext db, int pageSize, DateTime reportDate) {
        try {
            var pageIndex = 1;
            var clients = db.Clients
                .Where(x => x.CreatedTimestamp < reportDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);
            return clients.ToList();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error fetching unprocessed clients from database: {ex.Message}");
            return new List<ClientInformation>();
        }
    }

    internal static List<ClientInformation> FetchAllProcessedClients(StatsContext db, DateTime reportDate) {
        try {
            var clients = db.Clients
                .Where(x => x.CreatedTimestamp > reportDate);
            return clients.ToList();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error fetching processed clients from database: {ex.Message}");
            return new List<ClientInformation>();
        }
    }
}