using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Domain.Services;

namespace WMS.Application.Services;

public class BillingService(
    IRateRepository rateRepository,
    IReportRepository reportRepository,
    IPickTransactionRepository pickRepository,
    IReceivingTransactionRepository receivingRepository,
    IWithdrawalTransactionRepository withdrawalRepository,
    IVASTransactionRepository vasRepository) : IBillingService
{
    // Map internal ServiceTypes to Billing Tiers ---
    private static readonly Dictionary<ServiceType, string> StorageTierMap = new()
    {
        { ServiceType.FrozenStorage, "FrozenStorage" },
        { ServiceType.Chilling, "Chilling" },
        { ServiceType.Staging, "Staging" },
        { ServiceType.CoolStorage, "CoolStorage" },
        { ServiceType.DeepFrozenStorage, "DeepFrozen" },
        { ServiceType.ULTStorage, "ULT" }
    };

    public async Task<Invoice> GenerateInvoiceForAccountAsync(Guid accountId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken)
    {
        var invoice = Invoice.Create(accountId, periodStart, periodEnd);

        // Get pallet-day counts
        var palletDaysByZone = await reportRepository.GetDailyPalletCountByZoneAsync(accountId, periodStart, periodEnd, cancellationToken);

        // Get the total KG-Days for the same period
        var weightDaysByZone = await reportRepository.GetDailyWeightByZoneAsync(accountId, periodStart, periodEnd, cancellationToken);

        foreach (var zone in palletDaysByZone)
        {
            var serviceType = zone.Key;
            var totalPalletDays = zone.Value;

            if (totalPalletDays <= 0) continue;

            // Get the corresponding weight-days
            decimal totalWeightDays = weightDaysByZone.GetValueOrDefault(serviceType);

            // Find the string-based billing tier for this ServiceType
            if (StorageTierMap.TryGetValue(serviceType, out var tier))
            {
                // Let the helper method find the best rate (by Kg or by Pallet)
                await AddStorageChargeAsync(
                    invoice,
                    accountId,
                    tier,
                    totalPalletDays, // Pass pallet-days qty
                    totalWeightDays, // Pass kg-days qty
                    cancellationToken);
            }
        }

        // --- 2. Handling Charges (Unchanged) ---
        var receivingTransactions = await receivingRepository.GetForAccountByPeriodAsync(accountId, periodStart, periodEnd, cancellationToken);
        await AddChargeAsync(invoice, accountId, ServiceType.Handling, RateUom.Kg, null, receivingTransactions,
            txs => txs.SelectMany(r => r.Pallets).SelectMany(p => p.Lines).Sum(l => l.Weight),
            total => $"Handling (Inbound/Receiving) for {total:F2} kg.", cancellationToken);

        var pickTransactions = await pickRepository.GetForAccountByPeriodAsync(accountId, periodStart, periodEnd, cancellationToken);
        await AddChargeAsync(invoice, accountId, ServiceType.Handling, RateUom.Each, null, pickTransactions,
            txs => txs.Sum(p => p.Quantity),
            total => $"Handling (Picking) for {total} units.", cancellationToken);

        var withdrawalTransactions = await withdrawalRepository.GetForAccountByPeriodAsync(accountId, periodStart, periodEnd, cancellationToken);
        await AddChargeAsync(invoice, accountId, ServiceType.Handling, RateUom.Kg, null, withdrawalTransactions,
            txs => txs.Sum(w => w.TotalWeight),
            total => $"Handling (Outbound/Shipping) for {total:F2} kg.", cancellationToken);


        // --- 3. Value-Added Service Charges (Unchanged) ---
        var vasTransactions = await vasRepository.GetForAccountByPeriodAsync(accountId, periodStart, periodEnd, cancellationToken);

        // These services bill based on the MATERIAL lines (correct)
        await AddChargeAsync(invoice, accountId, ServiceType.Blasting, RateUom.Kg, null, vasTransactions.Where(v => v.ServiceType == ServiceType.Blasting),
            txs => txs.SelectMany(t => t.InputLines).Where(l => l.MaterialId.HasValue).Sum(l => l.Weight), // Added .Where() for safety
            total => $"Blast Freezing service for {total:F2} kg.", cancellationToken);

        await AddChargeAsync(invoice, accountId, ServiceType.Repack, RateUom.Each, null, vasTransactions.Where(v => v.ServiceType == ServiceType.Repack),
            txs => txs.SelectMany(t => t.InputLines).Where(l => l.MaterialId.HasValue).Sum(l => l.Quantity), // Added .Where() for safety
            total => $"Repackaging service for {total} units.", cancellationToken);

        await AddChargeAsync(invoice, accountId, ServiceType.Split, RateUom.Each, null, vasTransactions.Where(v => v.ServiceType == ServiceType.Split),
            txs => txs.SelectMany(t => t.InputLines).Where(l => l.MaterialId.HasValue).Sum(l => l.Quantity), // Added .Where() for safety
            total => $"Material Split service for {total} units.", cancellationToken);

        await AddChargeAsync(invoice, accountId, ServiceType.Labeling, RateUom.Each, null, vasTransactions.Where(v => v.ServiceType == ServiceType.Labeling),
            txs => txs.SelectMany(t => t.InputLines).Where(l => l.MaterialId.HasValue).Sum(l => l.Quantity), // Added .Where() for safety
            total => $"Compliance Labeling service for {total} units.", cancellationToken);

        await AddChargeAsync(invoice, accountId, ServiceType.Fumigation, RateUom.Cycle, null, vasTransactions.Where(v => v.ServiceType == ServiceType.Fumigation),
            txs => txs.Count(), // This logic is fine, it bills per transaction
            total => $"Fumigation/Quarantine service ({total} cycle(s)).", cancellationToken);

        // 1: CycleCount ---
        await AddChargeAsync(invoice, accountId, ServiceType.CycleCount, RateUom.Hour, null, vasTransactions.Where(v => v.ServiceType == ServiceType.CycleCount),
            txs => txs.SelectMany(t => t.InputLines).Where(l => !l.MaterialId.HasValue).Sum(l => l.Quantity), // Sum only service lines
            total => $"Inventory Cycle Count service ({total:F2} hours).", cancellationToken);

        // 2: CrossDock ---
        await AddChargeAsync(invoice, accountId, ServiceType.CrossDock, RateUom.Pallet, null, vasTransactions.Where(v => v.ServiceType == ServiceType.CrossDock),
            txs => txs.SelectMany(t => t.InputLines).Where(l => !l.MaterialId.HasValue).Sum(l => l.Quantity), // Sum only service lines
            total => $"Cross-Docking service for {total} pallet(s).", cancellationToken);

        // 3: Surcharge ---
        await AddChargeAsync(invoice, accountId, ServiceType.Surcharge, RateUom.Shipment, "Expedited", vasTransactions.Where(v => v.ServiceType == ServiceType.Surcharge),
            txs => txs.SelectMany(t => t.InputLines).Where(l => !l.MaterialId.HasValue).Sum(l => l.Quantity), // Sum only service lines
            total => $"Expedited Order Surcharge ({total} shipment(s)).", cancellationToken);

        // 4: Kitting (Labor) ---
        await AddChargeAsync(invoice, accountId, ServiceType.Kitting, RateUom.Hour, null, vasTransactions.Where(v => v.ServiceType == ServiceType.Kitting),
            txs => txs.SelectMany(t => t.InputLines).Where(l => !l.MaterialId.HasValue).Sum(l => l.Quantity), // Sum only service lines
            total => $"Kitting Labor ({total:F2} hours).", cancellationToken);

        // 5: Kitting (Per output materials)
        await AddChargeAsync(invoice, accountId, ServiceType.Kitting, RateUom.Each, null, vasTransactions.Where(v => v.ServiceType == ServiceType.Kitting),
            txs => txs.SelectMany(t => t.OutputLines).Sum(l => l.Quantity), // Sums the *output* kits
            total => $"Kitting Assembly Fee ({total} kits).", cancellationToken);

        invoice.FinalizeInvoice();
        return invoice;
    }

    // Helper for flexible storage billing ---
    private async Task AddStorageChargeAsync(
        Invoice invoice, Guid accountId, string tier,
        int totalPalletDays, decimal totalWeightDays,
        CancellationToken cancellationToken)
    {
        // 1. Try to find a rate for billing by KG
        var rateKg = await rateRepository.GetRateForAccountAsync(accountId, ServiceType.Storage, RateUom.Kg, tier, cancellationToken);
        if (rateKg != null && totalWeightDays > 0)
        {
            var description = $"{tier} Storage for {totalWeightDays:F2} kg-days.";
            invoice.AddLine(null, ServiceType.Storage, totalWeightDays, rateKg.Value, description, tier);
            return; // We found our charge (by KG), so we exit.
        }

        // 2. If no KG rate, fall back to billing by Pallet
        var ratePallet = await rateRepository.GetRateForAccountAsync(accountId, ServiceType.Storage, RateUom.Pallet, tier, cancellationToken);
        if (ratePallet != null && totalPalletDays > 0)
        {
            var description = $"{tier} Storage for {totalPalletDays} pallet-days.";
            invoice.AddLine(null, ServiceType.Storage, totalPalletDays, ratePallet.Value, description, tier);
            return; // We found our charge (by Pallet), so we exit.
        }

        // If no rate is found for either, no charge is added.
    }

    // Helper for VAS billing where quantity comes from transactions
    private async Task AddChargeAsync<T>(
        Invoice invoice, Guid accountId, ServiceType serviceType, RateUom uom, string? tier,
        IEnumerable<T> transactions, Func<IEnumerable<T>, decimal> quantitySelector,
        Func<decimal, string> descriptionFormatter, CancellationToken cancellationToken)
    {
        if (!transactions.Any()) return;

        var rate = await rateRepository.GetRateForAccountAsync(accountId, serviceType, uom, string.IsNullOrEmpty(tier) ? null : tier, cancellationToken);
        if (rate == null) return;

        var totalQuantity = quantitySelector(transactions);
        if (totalQuantity <= 0) return;

        var description = descriptionFormatter(totalQuantity);
        invoice.AddLine(null, serviceType, totalQuantity, rate.Value, description, tier ?? string.Empty);
    }

    // Helper for Storage or fixed quantity billing
    private async Task AddChargeAsync(
        Invoice invoice, Guid accountId, ServiceType serviceType, RateUom uom, string? tier,
        decimal totalQuantity, string description, CancellationToken cancellationToken)
    {
        if (totalQuantity <= 0) return;

        var rate = await rateRepository.GetRateForAccountAsync(accountId, serviceType, uom, string.IsNullOrEmpty(tier) ? null : tier, cancellationToken);
        if (rate == null) return;

        invoice.AddLine(null, serviceType, totalQuantity, rate.Value, description, tier ?? string.Empty);
    }
}