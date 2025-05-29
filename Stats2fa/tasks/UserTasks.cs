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

public class UserTasks {
    public static async Task PopulateUserInformation(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int maxUsers = 0, int counter = 0, int recursionDepth = 0) {
        // Maximum recursion depth to prevent stack overflow
        const int MaxRecursionDepth = 1000;
        if (recursionDepth > MaxRecursionDepth) {
            StatsLogger.Log(stats: apiInformation, $"Maximum recursion depth reached ({MaxRecursionDepth}). Stopping client population to prevent infinite loop.");
            return;
        }

        // Maximum clients check
        var pageSize = 100;
        if (maxUsers > 0 && counter >= maxUsers) {
            StatsLogger.Log(stats: apiInformation, $"Reached maximum specified users ({maxUsers}). Stopping further processing.");
            return;
        }

        // Fetch the unprocessed users in batches
        try {
            StatsLogger.Log(stats: apiInformation, $"Fetching users < {reportDate:s} (Batch {recursionDepth + 1})");
            var users = FetchUnprocessedUsers(db: db, pageSize: pageSize, reportDate: reportDate, apiInformation: apiInformation);
            StatsLogger.Log(stats: apiInformation, $"Fetching {users.Count} Users to update");
            if (users.Any()) {
                var processedCount = 0;
                try {
                    // Limit parallelism to avoid overwhelming the API and causing timeouts
                    var parallelOptions = new ParallelOptions {
                        MaxDegreeOfParallelism = Math.Min(5, val2: Environment.ProcessorCount)
                    };

                    await Parallel.ForEachAsync(source: users,
                        parallelOptions: parallelOptions,
                        async (userInformation, cancellationToken) =>
                        {
                            try {
                                await GetUserInformation(httpClient: httpClient, apiInformation: apiInformation, userInformation: userInformation, cancellationToken: cancellationToken);
                                Interlocked.Increment(location: ref processedCount);

                                // Log progress periodically
                                var current = Interlocked.CompareExchange(location1: ref processedCount, 0, 0);
                                if (current % 10 == 0) StatsLogger.Log(stats: apiInformation, $"Progress: Processed {current}/{users.Count} users");
                            }
                            catch (Exception ex) {
                                StatsLogger.Log(stats: apiInformation, $"Error processing user {userInformation.UserId}: {ex.Message}");
                                // Continue with other users despite this error
                            }
                        });

                    StatsLogger.Log(stats: apiInformation, $"Completed processing {processedCount}/{users.Count} clients in batch {recursionDepth + 1}");

                    try {
                        StatsLogger.Log(stats: apiInformation, "Saving changes to database...");
                        await db.SaveChangesAsync();
                        counter += users.Count;
                        StatsLogger.Log(stats: apiInformation, $"Checkpointed {users.Count} users");
                    }
                    catch (Exception ex) {
                        StatsLogger.Log(stats: apiInformation, $"Error saving user data to database: {ex.Message}");
                    }

                    // Continue with next batch with increment of recursion depth
                    await PopulateUserInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, maxUsers: maxUsers, counter: counter, recursionDepth + 1);
                }
                catch (Exception ex) {
                    StatsLogger.Log(stats: apiInformation, $"Error during parallel client processing: {ex.Message}");
                    // Try to continue with the next batch
                    counter += users.Count;
                    await PopulateUserInformation(httpClient: httpClient, apiInformation: apiInformation, db: db, reportDate: reportDate, maxUsers: maxUsers, counter: counter, recursionDepth + 1);
                }
            }
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error fetching unprocessed users: {ex.Message}");
        }
    }

    private static async ValueTask GetUserInformation(HttpClient httpClient, ApiInformation apiInformation, UserInformation userInformation, CancellationToken cancellationToken) {
        try {
            // Create a new linked cancellation token source that combines our token with a timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token: cancellationToken);
            // Set a timeout of 60 seconds for each client operation
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

            var tasks = new List<Task> {
                GetUserSettings(httpClient: httpClient, apiInformation: apiInformation, userInformation: userInformation, cancellationToken: timeoutCts.Token)
            };

            await Task.WhenAll(tasks: tasks);
            userInformation.CreatedTimestamp = DateTime.UtcNow;
        }
        catch (OperationCanceledException) {
            // Handle cancellation gracefully
            StatsLogger.Log(stats: apiInformation, "Operation for client was cancelled", client: userInformation.UserId);
            userInformation.UserStatsStatus = "ERROR_TIMEOUT";
            throw;
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error in GetClientInformation for client: {ex.Message}", client: userInformation.UserId);
            userInformation.UserStatsStatus = "ERROR_EXCEPTION";
            throw;
        }
    }

    private static async Task GetUserSettings(HttpClient httpClient, ApiInformation apiInformation, UserInformation userInformation, CancellationToken cancellationToken) {
        apiInformation.ApiCallsUsers++;
        apiInformation.LastUpdated = DateTime.UtcNow;

        // GET https://api.staging.kt1.io/fleet/v2/accounts/users/17e62563-a1c1-41cd-9718-c480b4e45952
        var url = $"accounts/users/{userInformation.UserId}";
        User response;

        try {
            // Use GetAsync with the cancellation token for proper cancellation support
            var httpResponse = await httpClient.GetAsync(requestUri: url, cancellationToken: cancellationToken);

            // Check if the request was successful
            if (!httpResponse.IsSuccessStatusCode) {
                StatsLogger.Log(stats: apiInformation, $"HTTP error {httpResponse.StatusCode} getting user {userInformation.UserId}. URL: {httpClient.BaseAddress}{url}");
                return;
            }

            // Check content type to ensure it's JSON
            var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
            if (contentType == null || !contentType.Contains("application/json")) {
                StatsLogger.Log(stats: apiInformation, $"Unexpected content type: {contentType} for user {userInformation.UserId}. URL: {httpClient.BaseAddress}{url}");

                // For debugging: try to read the content as string to see what's being returned
                if (contentType?.Contains("text/html") == true)
                    try {
                        var htmlContent = await httpResponse.Content.ReadAsStringAsync();
                        var preview = htmlContent.Length > 100 ? htmlContent.Substring(0, 100) + "..." : htmlContent;
                        StatsLogger.Log(stats: apiInformation, $"HTML response preview: {preview}");

                        userInformation.UserStatsStatus = "ERROR_HTML_RESPONSE";
                    }
                    catch (Exception ex) {
                        StatsLogger.Log(stats: apiInformation, $"Error reading HTML content: {ex.Message}");
                    }

                return;
            }

            // Read as JSON
            var responseText = await httpResponse.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
            Console.WriteLine(value: responseText);

            // Read as JSON
            response = await httpResponse.Content.ReadFromJsonAsync<User>() ?? new User();
        }
        catch (JsonException jsonEx) {
            StatsLogger.Log(stats: apiInformation, $"JSON parsing error getting client. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: jsonEx.Message);
            return;
        }
        catch (TaskCanceledException tcEx) {
            StatsLogger.Log(stats: apiInformation, $"Request timeout or cancellation getting client. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: tcEx.Message);
            return;
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error getting client. URL: {httpClient.BaseAddress}{url}");
            StatsLogger.Log(stats: apiInformation, message: ex.Message);
            return;
        }

        // Safely set properties with null checks to avoid NullReferenceException
        try {
            if (response.Otp != null) {
                var otp = response.Otp.FirstOrDefault(); // we are only handling the first otp (if there is any)
                if (otp?.Verified != null) userInformation.TotpVerified = otp.Verified;
                if (otp?.Type != null) userInformation.TotpType = otp.Type;
                if (otp?.Date != null) userInformation.TotpDate = otp.Date;
            }

            userInformation.UserStatsStatus = "SUCCESS";
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error processing user data for {userInformation.UserId}: {ex.Message}", client: userInformation.UserId);
        }
    }

    internal static List<UserInformation> FetchUnprocessedUsers(StatsContext db, int pageSize, DateTime reportDate, ApiInformation? apiInformation) {
        try {
            var pageIndex = 1;
            var clients = db.Users
                // .Where(x => x.CreatedTimestamp < reportDate)
                .Where(x => !x.UserStatsStatus.Equals("SUCCESS"))
                .Skip((pageIndex - 1) * pageSize)
                .Take(count: pageSize);
            return clients.ToList();
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error fetching unprocessed users from database: {ex.Message}");
            return new List<UserInformation>();
        }
    }

    internal static List<UserInformation> FetchAllProcessedUsers(StatsContext db, DateTime reportDate, ApiInformation? apiInformation) {
        try {
            var items = db.Users
                .Where(x => x.CreatedTimestamp > reportDate);
            return items.ToList();
        }
        catch (Exception ex) {
            StatsLogger.Log(stats: apiInformation, $"Error fetching processed clients from database: {ex.Message}");
            return new List<UserInformation>();
        }
    }
}