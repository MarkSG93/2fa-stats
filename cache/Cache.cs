using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stats2fa.api.models;
using Stats2fa.database;

namespace Stats2fa.cache;

internal class Cache {
    public static async Task SaveDistributors(StatsContext db, Distributors distributors, DateTime reportDate)
    {
        int progressCounter = 1;

        foreach (Distributor distributor in distributors.DistributorList) {
            var recordExists = false;
            var updateRequired = true;

            var distribInfo = await db.Distributors.SingleOrDefaultAsync(x => x.DistributorInformationId == StatsContext.Guid2Int(distributor.Id));

            if (distribInfo != null) {
                recordExists = true;
                updateRequired = distribInfo.CreatedTimestamp < reportDate;
            }

            var state = updateRequired ? "preparing" : "skipping";
            // Console.WriteLine($"[{DateTime.UtcNow:s}][   ][{distributor.Id}][{Guid.Empty}][{Guid.Empty}] {state} distributor\t({progressCounter++:00000}/{distributors.DistributorList.Count:00000})");

            if (!recordExists) {
                distribInfo = new DistributorInformation {
                    DistributorInformationId = StatsContext.Guid2Int(new Guid(distributor.Id)),
                    CreatedTimestamp = DateTime.MinValue,
                    DistributorId = distributor.Id,
                    DistributorName = distributor.Name,
                    DistributorType = distributor.Type,
                    distributorStatus = distributor.State
                };
            }

            // Save to DB
            try {
                if (!recordExists) await db.Distributors.AddAsync(distribInfo);
            }
            catch (Exception e) {
                Console.WriteLine($"[{DateTime.UtcNow:s}][   ][{distributor.Id}][{Guid.Empty}][{Guid.Empty}] error saving distributor");
            }
        }

        // Save any changes
        await db.SaveChangesAsync();
    }

    public static async Task SaveVendors(StatsContext db, ConcurrentBag<Vendor> allVendors, DateTime reportDate)
    {
        int progressCounter = 1;
        foreach (Vendor vendor in allVendors) {
            var recordExists = false;
            var updateRequired = true;

            var vendorInformation = await db.Vendors.SingleOrDefaultAsync(x => x.VendorInformationId == StatsContext.Guid2Int(vendor.Id));

            if (vendorInformation != null) {
                recordExists = true;
                updateRequired = vendorInformation.CreatedTimestamp < reportDate;
            }

            var state = updateRequired ? "preparing" : "skipping";
            // Console.WriteLine($"[{DateTime.UtcNow:s}][   ][{vendor.owner.Id}][{vendor.Id}][{Guid.Empty}] {state} vendor\t\t({progressCounter++:00000}/{allVendors.Count:00000})");

            if (!recordExists) {
                vendorInformation = new VendorInformation {
                    VendorInformationId = StatsContext.Guid2Int(vendor.Id),
                    CreatedTimestamp = DateTime.MinValue,
                    VendorId = vendor.Id,
                    VendorName = vendor.Name,
                    VendorType = vendor.Type,
                    VendorStatus = vendor.State,
                    VendorDistributorId = vendor.owner.Id
                };
            }

            // Save to DB
            try {
                if (!recordExists) await db.Vendors.AddAsync(vendorInformation!);
            }
            catch (Exception e) {
                Console.WriteLine($"[{DateTime.UtcNow:s}][   ][{vendorInformation.VendorDistributorId}][{vendorInformation.VendorId}][{Guid.Empty}] error saving vendor");
            }
        }

        // Save any changes
        await db.SaveChangesAsync();
    }

    public static async Task SaveClients(StatsContext db, ConcurrentBag<Client> allClients, DateTime reportDate)
    {
        int progressCounter = 1;

        var temp = allClients.ToList();
        List<Client> sortedList = temp.OrderBy(o => o.Id).Distinct(ClientComparer.Instance).ToList();
        allClients = new ConcurrentBag<Client>(sortedList);


        foreach (Client client in allClients) {
            var recordExists = false;
            var updateRequired = true;
            var clientIndex = StatsContext.Guid2Int(client.Id);
            // try get cached client
            ClientInformation? clientInformation = db.Clients.SingleOrDefault(x => x.ClientInformationId == clientIndex);

            if (clientInformation != null) {
                recordExists = true;
                updateRequired = clientInformation.CreatedTimestamp < reportDate;
            }

            var state = updateRequired ? "preparing" : "skipping";
            // Console.WriteLine($"[{DateTime.UtcNow:s}][   ][{Guid.Empty}][{client.Owner.Id}][{client.Id}] {state} client\t\t({progressCounter++:00000}/{allClients.Count:00000})");

            if (!recordExists) {
                clientInformation = new ClientInformation {
                    ClientInformationId = clientIndex,
                    CreatedTimestamp = DateTime.MinValue,
                    ClientId = client.Id,
                    ClientName = client.Name,
                    ClientType = client.Type,
                    ClientStatus = client.State,
                    ClientVendorId = client.Owner.Id
                };
            }

            // Save to DB
            try {
                if (!recordExists) await db.Clients.AddAsync(clientInformation!);
            }
            catch (Exception e) {
                Console.WriteLine($"Error saving to cache {JsonSerializer.Serialize(clientInformation)}, {e?.InnerException?.Message}");
            }
        }

        // Save any changes
        await db.SaveChangesAsync();
    }
}