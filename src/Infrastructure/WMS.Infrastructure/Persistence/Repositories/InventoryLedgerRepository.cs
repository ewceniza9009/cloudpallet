using Dapper; // Uses Dapper
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data; // For CommandType
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Persistence.Repositories;

public class InventoryLedgerRepository : IInventoryLedgerRepository
{
    private readonly WmsDbContext _context;

    public InventoryLedgerRepository(WmsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// This method calls the stored procedure 'sp_InsertInventoryLedgerEntry'
    /// </summary>
    public async Task AddLedgerEntryAsync(
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
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var transaction = _context.Database.CurrentTransaction?.GetDbTransaction();

        var parameters = new
        {
            Timestamp = timestamp,
            MaterialId = materialId,
            AccountId = accountId,
            TransactionType = transactionType.ToString(),
            TransactionId = transactionId,
            QuantityChange = quantityChange,
            WeightChange = weightChange,
            MaterialName = materialName,
            AccountName = accountName,
            DocumentReference = documentReference,
            LocationId = locationId,
            PalletId = palletId,
            SupplierId = supplierId,
            TruckId = truckId,
            UserId = userId,
            UserName = userName
        };

        // *** THIS IS THE STORED PROCEDURE CALL ***
        await connection.ExecuteAsync(
            "dbo.sp_InsertInventoryLedgerEntry",
            parameters,
            transaction: transaction,
            commandType: CommandType.StoredProcedure
        );
    }
}