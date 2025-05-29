using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stats2fa.api;
using Stats2fa.api.models;
using Stats2fa.database;
using Stats2fa.logger;

namespace Stats2fa.cache;

internal class Cache {
    public static async Task SaveDistributors(StatsContext db, Distributors distributors, DateTime reportDate, ApiInformation? apiInformation) {
        var progressCounter = 1;

        foreach (var distributor in distributors.DistributorList) {
            var recordExists = false;
            var updateRequired = true;

            var distribInfo = await db.Distributors.SingleOrDefaultAsync(x => x.DistributorInformationId == StatsContext.Guid2Int(distributor.Id));

            if (distribInfo != null) {
                recordExists = true;
                updateRequired = distribInfo.CreatedTimestamp < reportDate;
            }

            var state = updateRequired ? "preparing" : "skipping";
            // StatsLogger.Log(apiInformation,$"[{DateTime.UtcNow:s}][   ][{distributor.Id}][{Guid.Empty}][{Guid.Empty}] {state} distributor\t({progressCounter++:00000}/{distributors.DistributorList.Count:00000})");

            if (!recordExists)
                distribInfo = new DistributorInformation {
                    DistributorInformationId = StatsContext.Guid2Int(new Guid(g: distributor.Id)),
                    CreatedTimestamp = DateTime.MinValue,
                    DistributorId = distributor.Id,
                    DistributorName = distributor.Name,
                    DistributorType = distributor.Type,
                    DistributorStatus = distributor.State
                };

            // Save to DB
            try {
                if (!recordExists) await db.Distributors.AddAsync(entity: distribInfo);
            }
            catch (Exception e) {
                StatsLogger.Log(stats: apiInformation, $"[{DateTime.UtcNow:s}][   ][{distributor.Id}][{Guid.Empty}][{Guid.Empty}] error saving distributor");
            }
        }

        // Save any changes
        await db.SaveChangesAsync();
    }

    public static async Task SaveVendors(StatsContext db, ConcurrentBag<Vendor> allVendors, DateTime reportDate, ApiInformation? apiInformation) {
        var progressCounter = 1;
        foreach (var vendor in allVendors) {
            var recordExists = false;
            var updateRequired = true;

            var vendorInformation = await db.Vendors.SingleOrDefaultAsync(x => x.VendorInformationId == StatsContext.Guid2Int(vendor.Id));

            if (vendorInformation != null) {
                recordExists = true;
                updateRequired = vendorInformation.CreatedTimestamp < reportDate;
            }

            var state = updateRequired ? "preparing" : "skipping";
            // StatsLogger.Log(apiInformation,$"[{DateTime.UtcNow:s}][   ][{vendor.owner.Id}][{vendor.Id}][{Guid.Empty}] {state} vendor\t\t({progressCounter++:00000}/{allVendors.Count:00000})");

            if (!recordExists)
                vendorInformation = new VendorInformation {
                    VendorInformationId = StatsContext.Guid2Int(value: vendor.Id),
                    CreatedTimestamp = DateTime.MinValue,
                    VendorId = vendor.Id,
                    VendorName = vendor.Name,
                    VendorType = vendor.Type,
                    VendorStatus = vendor.State,
                    VendorDistributorId = vendor.owner.Id
                };

            // Save to DB
            try {
                if (!recordExists) await db.Vendors.AddAsync(vendorInformation!);
            }
            catch (Exception e) {
                StatsLogger.Log(stats: apiInformation, $"[{DateTime.UtcNow:s}][   ][{vendorInformation.VendorDistributorId}][{vendorInformation.VendorId}][{Guid.Empty}] error saving vendor");
            }
        }

        // Save any changes
        await db.SaveChangesAsync();
    }

    public static async Task SaveClients(StatsContext db, ConcurrentBag<Client> allClients, DateTime reportDate, ApiInformation? apiInformation) {
        var progressCounter = 1;

        var temp = allClients.ToList();
        var sortedList = temp.OrderBy(o => o.Id).Distinct(comparer: ClientComparer.Instance).ToList();
        allClients = new ConcurrentBag<Client>(collection: sortedList);


        foreach (var client in allClients) {
            var recordExists = false;
            var updateRequired = true;
            var clientIndex = StatsContext.Guid2Int(value: client.Id);
            // try get cached client
            var clientInformation = db.Clients.SingleOrDefault(x => x.ClientInformationId == clientIndex);

            if (clientInformation != null) {
                recordExists = true;
                updateRequired = clientInformation.CreatedTimestamp < reportDate;
            }

            var state = updateRequired ? "preparing" : "skipping";
            // StatsLogger.Log(apiInformation,$"[{DateTime.UtcNow:s}][   ][{Guid.Empty}][{client.Owner.Id}][{client.Id}] {state} client\t\t({progressCounter++:00000}/{allClients.Count:00000})");

            if (!recordExists)
                clientInformation = new ClientInformation {
                    ClientInformationId = clientIndex,
                    CreatedTimestamp = DateTime.MinValue,
                    ClientId = client.Id,
                    ClientName = client.Name,
                    ClientType = client.Type,
                    ClientStatus = client.State,
                    ClientVendorId = client.Owner.Id
                };

            // Save to DB
            try {
                if (!recordExists) await db.Clients.AddAsync(clientInformation!);
            }
            catch (Exception e) {
                StatsLogger.Log(stats: apiInformation, $"Error saving to cache {JsonSerializer.Serialize(value: clientInformation)}, {e?.InnerException?.Message}");
            }
        }

        // Save any changes
        await db.SaveChangesAsync();
    }

    public static async Task SaveUsers(StatsContext db, ConcurrentBag<User> allUsers, DateTime reportDate, ApiInformation? apiInformation) {
        var progressCounter = 1;

        var temp = allUsers.ToList();
        var sortedList = temp.OrderBy(o => o.Id).Distinct(comparer: UserComparer.Instance).ToList();
        allUsers = new ConcurrentBag<User>(collection: sortedList);


        foreach (var user in allUsers) {
            var recordExists = false;
            var updateRequired = true;
            var userIndex = StatsContext.Guid2Int(value: user.Id);
            // try get cached client
            var userInformation = db.Users.SingleOrDefault(x => x.UserInformationId == userIndex);

            if (userInformation != null) {
                recordExists = true;
                updateRequired = userInformation.CreatedTimestamp < reportDate;
            }

            var state = updateRequired ? "preparing" : "skipping";
            // StatsLogger.Log(apiInformation,$"[{DateTime.UtcNow:s}][   ][{Guid.Empty}][{client.Owner.Id}][{client.Id}] {state} client\t\t({progressCounter++:00000}/{allClients.Count:00000})");

            if (!recordExists)
                userInformation = new UserInformation {
                    UserInformationId = userIndex,
                    UserId = user.Id,
                    Name = user.Name ?? string.Empty,
                    Email = user.EmailAddress ?? string.Empty,
                    Mobile = user.Mobile,
                    TimeZone = user.TimeZoneId,
                    Language = user.Language,
                    State = user.State,
                    OwnerId = user.Owner?.Id ?? string.Empty,
                    OwnerName = user.Owner?.Name ?? string.Empty,
                    OwnerType = user.Owner?.Type ?? string.Empty,
                    DefaultClientId = user.DefaultClient?.Id ?? string.Empty,
                    DefaultClientName = user.DefaultClient?.Name ?? string.Empty,
                    CostCentreId = string.IsNullOrEmpty(value: user.CostCentre?.Id) || user.CostCentre?.Id == "00000000-0000-0000-0000-000000000000" ? null : user.CostCentre?.Id,
                    CostCentreName = string.IsNullOrEmpty(value: user.CostCentre?.Id) || user.CostCentre?.Id == "00000000-0000-0000-0000-000000000000" ? null : user.CostCentre?.Name,
                    ModifiedDate = user.ModifiedDate,
                    CreatedTimestamp = DateTime.UtcNow,
                    UserStatsStatus = string.Empty
                };

            // Save to DB
            try {
                if (!recordExists) await db.Users.AddAsync(userInformation!);
            }
            catch (Exception e) {
                StatsLogger.Log(stats: apiInformation, $"Error saving to cache {JsonSerializer.Serialize(value: userInformation)}, {e?.InnerException?.Message}");
            }
        }

        // Save any changes
        await db.SaveChangesAsync();
    }
}