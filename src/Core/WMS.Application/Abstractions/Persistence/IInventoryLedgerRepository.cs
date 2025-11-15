// ---- File: src/Application/WMS.Application/Abstractions/Persistence/IInventoryLedgerRepository.cs ----
// [COMPLETE NEW FILE]

using WMS.Domain.Entities;
using WMS.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WMS.Application.Abstractions.Persistence;

/// <summary>
/// Interface for repository operations specific to InventoryLedgerEntry entities.
/// </summary>
public interface IInventoryLedgerRepository
{
    /// <summary>
    /// Adds a new inventory ledger entry by calling the dedicated stored procedure.
    /// The actual save occurs when IUnitOfWork.SaveChangesAsync is called.
    /// </summary>
    /// <param name="timestamp">The exact timestamp of the transaction.</param>
    /// <param name="materialId">The ID of the material being moved.</param>
    /// <param name="accountId">The ID of the account that owns the material.</param>
    /// <param name="transactionType">The type of transaction (e.g., Receiving, Picking).</param>
    /// <param name="transactionId">The source transaction ID (e.g., PickTransactionId, ReceivingId).</param>
    /// <param name="quantityChange">The change in quantity (positive for IN, negative for OUT).</param>
    /// <param name="weightChange">The change in weight (positive for IN, negative for OUT).</param>
    /// <param name="materialName">Denormalized material name.</param>
    /// <param name="accountName">Denormalized account name.</param>
    /// <param name="documentReference">User-friendly document reference (e.g., RECV-123).</param>
    /// <param name="locationId">Optional: The location ID associated with the movement.</param>
    /// <param name="palletId">Optional: The pallet ID associated with the movement.</param>
    /// <param name="supplierId">Optional: The supplier ID (for receiving).</param>
    /// <param name="truckId">Optional: The truck ID (for receiving/shipping).</param>
    /// <param name="userId">Optional: The user who triggered the event.</param>
    /// <param name="userName">Optional: Denormalized user name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task AddLedgerEntryAsync(
        DateTime timestamp,
        Guid materialId,
        Guid accountId,
        LedgerTransactionType transactionType,
        Guid transactionId,
        decimal quantityChange,
        decimal weightChange,
        string materialName,
        string accountName,
        string documentReference,
        Guid? locationId,
        Guid? palletId,
        Guid? supplierId,
        Guid? truckId,
        Guid? userId,
        string? userName,
        CancellationToken cancellationToken);
}