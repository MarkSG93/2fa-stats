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

    private static async Task GetClientUsers(HttpClient httpClient, ApiInformation apiInformation, ClientInformation clientInformation, CancellationToken cancellationToken) {
        // todo implement https://api.staging.kt1.io/fleet/v2/accounts/users?owner=00000000-0000-0000-0000-000000000000&offset=0&limit=100&sort=name:asc&filter=(state=inactive|state=active|state=suspended)
        // sample response {"offset":0,"limit":100,"count":23,"items":[{"id":"e9a4d9f2-754f-42ca-81fa-d63db38b845b","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Ben Payne","emailAddress":"ben.payne@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/12/18 14:28:20"},{"id":"696d5d7d-6e2d-4080-88cc-e33d6c7669d5","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Bob Example","emailAddress":"bob@example.com","mobile":"+27","timeZoneId":"Africa/Johannesburg","language":"en-us","state":"suspended","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2025/04/02 07:46:08"},{"id":"53ede87c-7736-444e-98f3-9f51c5431670","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Brent Robarts","emailAddress":"brent@keytelematics.com","mobile":"+27","timeZoneId":"Africa/Johannesburg","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/10/16 13:24:22"},{"id":"12732de2-1ace-460a-bb4b-b313cdeb34ab","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Dan Pikker","emailAddress":"daniel@keytelematics.com","mobile":"+27","timeZoneId":"Africa/Johannesburg","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/11/15 06:54:05"},{"id":"f57bae21-ec57-4076-a6e9-75564b65017c","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Data Transfer Platform","emailAddress":"dtp@fake.radius.solutions","timeZoneId":"GMT+0","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/09/16 14:34:38"},{"id":"8e07ce7a-1dc5-4a97-b8e5-3dc9fb8f2e04","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"David Vannucci","emailAddress":"davidv@keytelematics.com","mobile":"+27","timeZoneId":"Africa/Johannesburg","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2025/02/06 10:33:04"},{"id":"c71f9ddc-6ff8-4ede-8e0b-b3802de47d3a","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Dirk Lotz","emailAddress":"dirk.lotz@keytelematics.com","timeZoneId":"Africa/Johannesburg","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2025/04/09 08:58:05"},{"id":"0de73d05-467b-4895-8b7a-92d306763f07","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Eddie Beard","emailAddress":"eddie.beard@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/08/30 09:23:47"},{"id":"0c6ae859-527b-4267-8bf6-56713ff38735","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Gavin van Gent","emailAddress":"gavin.vangent@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2025/04/02 07:46:27"},{"id":"8c498afa-e994-4e76-919e-89f215c724aa","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Holly Pepper","emailAddress":"holly.pepper@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/12/20 11:48:11"},{"id":"21c34aa6-504b-4987-ae22-3c4c517ffab4","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Joe Cumbo","emailAddress":"joe.cumbo@radius.com","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"0ffe2dd5-58fa-4539-a25d-62eaa1f7108a","name":"Device Testing Client"},"costCentre":null,"modifiedDate":"2025/03/10 11:34:25"},{"id":"f0f99f47-2392-4885-9d83-4193174aa0e1","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Johan Havenga (S)","emailAddress":"johanh@keytelematics.com","mobile":"+27","timeZoneId":"Africa/Harare","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2025/01/27 07:24:34"},{"id":"fc503643-bce2-4c34-b8e0-607c6ad6b452","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Jon Greatbatch","emailAddress":"jon.greatbatch@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/12/20 13:18:34"},{"id":"119047c1-e835-467d-8775-f1eabfd8630a","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Kevin Gill","emailAddress":"kevin.gill@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/12/05 06:10:47"},{"id":"d1aa57e6-27de-4384-977f-70e9f60bb119","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Mark Dodd","emailAddress":"mark.dodd@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/12/03 09:16:37"},{"id":"61de3652-6c6a-42e4-89fd-1bd472e024a2","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Mark Griffiths","emailAddress":"mark.griffiths@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/12/04 10:51:03"},{"id":"e5a1076c-4bfe-4fcd-abe0-4681fdcc1dec","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Nathan Walters","emailAddress":"nathan.walters@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2025/01/20 11:06:12"},{"id":"7550406a-63a3-44e7-8184-6571e700ec60","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Nick Cox","emailAddress":"nick.cox@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/08/30 09:32:49"},{"id":"d4990ada-a642-4040-8e30-6c96a96c1217","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Raj Virk","emailAddress":"raj.virk@radius.com","mobile":"+44","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/09/30 09:47:50"},{"id":"a760cf05-a5cc-4fb9-998d-6f893051ad43","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Riaan Van Niekerk","emailAddress":"riaan@keytelematics.com","mobile":"+27","timeZoneId":"Africa/Johannesburg","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2025/03/06 07:36:22"},{"id":"dedd6067-fcfb-4f1c-b9ae-fb715f57d182","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Russell van der Walt","emailAddress":"russell@keytelematics.com","timeZoneId":"Europe/London","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2024/11/22 14:34:02"},{"id":"a4329acb-a8b9-4b68-b163-fafadccd1a2b","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"Walter Klosta","emailAddress":"walter.klosta@radius.com","mobile":"+27790000001","timeZoneId":"Africa/Johannesburg","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2025/01/30 10:08:45"},{"id":"dd378831-88bd-452e-ba24-bf620cb97a97","owner":{"id":"00000000-0000-0000-0000-000000000000","name":"SYSTEM","type":"system"},"name":"⚠️ System Level Users to be approved by Jon","emailAddress":"system@example.com","mobile":"+27","timeZoneId":"Africa/Johannesburg","language":"en-us","state":"active","defaultClient":{"id":"d4d4627b-f436-4d97-a25d-057d56d23605","name":"Example Inc"},"costCentre":null,"modifiedDate":"2025/03/06 07:35:41"}]}
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
            if (response != null) clientInformation.ClientUsers = response;

            clientInformation.ClientStatsStatus = "SUCCESS";
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error processing user list for {clientInformation.ClientId}: {ex.Message}", client: clientInformation.ClientId);
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