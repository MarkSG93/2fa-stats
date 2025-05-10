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
    public static async Task PopulateClientInformation(HttpClient httpClient, ApiInformation apiInformation, StatsContext db, DateTime reportDate, int maxClients = 0, int counter = 0) {
        var pageSize = 100;
        if (counter > maxClients) return;

        // httpClient.Timeout = TimeSpan.FromSeconds(10);
        Console.WriteLine($"Fetching clients < {reportDate:s}");
        List<ClientInformation> clients = FetchUnprocessedClients(db, pageSize, reportDate);
        Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"fetching {clients.Count} clients to update"));
        if (clients.Any()) {
            await Parallel.ForEachAsync(source: clients, (client, cancellationToken) => GetClientInformation(httpClient, apiInformation, client, cancellationToken));
            await db.SaveChangesAsync();

            counter += clients.Count();

            Console.WriteLine("\n" + StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"checkpointed {pageSize} clients"));
            await PopulateClientInformation(httpClient, apiInformation, db, reportDate, counter);
        }
        else {
            Console.WriteLine(StringUtils.Log(DateTime.UtcNow, null, null, null, null, apiInformation, $"PopulateClientInformation complete {counter} prepared clients"));
        }
    }

    private static async ValueTask GetClientInformation(HttpClient httpClient, ApiInformation apiInformation, ClientInformation client, CancellationToken cancellationToken) {
        var tasks = new List<Task> {
            GetClientInformationAndSettings(httpClient, apiInformation, client),
        };
        await Task.WhenAll(tasks);
        client.CreatedTimestamp = DateTime.UtcNow;
    }
    
    private static async Task GetClientInformationAndSettings(HttpClient httpClient, ApiInformation apiInformation, ClientInformation clientInformation) {
        apiInformation.ApiCallsClients++;
        apiInformation.LastUpdated = DateTime.UtcNow;
        var response = await httpClient.GetFromJsonAsync<Client>($"accounts/clients/{clientInformation.ClientId}");
       
        // Safely set properties with null checks to avoid NullReferenceException
        try {
            if (response.passwordPolicy != null) {
                if (response.passwordPolicy.Source != null) {
                    clientInformation.VendorPasswordPolicySourceId = response.passwordPolicy.Source.Id;
                    clientInformation.VendorPasswordPolicySourceName = response.passwordPolicy.Source.Name;
                    clientInformation.VendorPasswordPolicySourceType = response.passwordPolicy.Source.Type;
                }

                clientInformation.VendorPasswordPolicyPasswordLength = response.passwordPolicy.PasswordLength;

                if (response.passwordPolicy.PasswordComplexity != null) {
                    clientInformation.VendorPasswordPolicyPasswordComplexityMixedcase = response.passwordPolicy.PasswordComplexity.MixedCase;
                    clientInformation.VendorPasswordPolicyPasswordComplexityAlphanumerical = response.passwordPolicy.PasswordComplexity.AlphaNumerical;
                    clientInformation.VendorPasswordPolicyPasswordComplexityNocommonpasswords = response.passwordPolicy.PasswordComplexity.NoCommonPasswords;
                    clientInformation.VendorPasswordPolicyPasswordComplexitySpecialcharacters = response.passwordPolicy.PasswordComplexity.SpecialCharacters;
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(response.passwordPolicy));
                }

                clientInformation.VendorPasswordPolicyPasswordExpirationDays = response.passwordPolicy.PasswordExpirationDays;

                if (response.passwordPolicy.OtpSettings != null) {
                    if (response.passwordPolicy.OtpSettings.Methods != null) {
                        if (response.passwordPolicy.OtpSettings.Methods.Totp != null) {
                            clientInformation.VendorPasswordPolicyOtpSettingsMethodsTotpTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Totp.TokenValidityDays;
                        }

                        if (response.passwordPolicy.OtpSettings.Methods.Email != null) {
                            clientInformation.VendorPasswordPolicyOtpSettingsMethodsEmailTokenValidityDays = response.passwordPolicy.OtpSettings.Methods.Email.TokenValidityDays;
                        }
                    }

                    clientInformation.VendorPasswordPolicyOtpSettingsGracePeriodDays = response.passwordPolicy.OtpSettings.GracePeriodDays;
                    clientInformation.VendorPasswordPolicyOtpSettingsMandatoryFor = response.passwordPolicy.OtpSettings.MandatoryFor;
                }
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error processing vendor data for {clientInformation.ClientId}: {ex.Message}");
        }
        
    }

    
    internal static List<ClientInformation> FetchUnprocessedClients(StatsContext db, int pageSize, DateTime reportDate) {
        var pageIndex = 1;
        var clients = db.Clients
            .Where(x => x.CreatedTimestamp < reportDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);
        return clients.ToList();
    }

    internal static List<ClientInformation> FetchAllProcessedClients(StatsContext db, DateTime reportDate) {
        var clients = db.Clients
            .Where(x => x.CreatedTimestamp > reportDate);
        return clients.ToList();
    }
}