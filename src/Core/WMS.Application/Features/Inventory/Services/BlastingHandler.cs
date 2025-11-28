using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Inventory.Commands;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Inventory.Services;

public class BlastingHandler(
    IReceivingTransactionRepository receivingRepository,
    IVASTransactionRepository vasRepository,
    IUnitOfWork unitOfWork) : IVasServiceHandler
{
    public ServiceType ServiceType => ServiceType.Blasting;

    public async Task HandleAsync(RecordVasCommand command, CancellationToken cancellationToken)
    {
        if (!command.PalletId.HasValue) throw new InvalidOperationException("PalletId is required for Blasting.");

        var pallet = await receivingRepository.GetPalletWithLinesByIdAsync(command.PalletId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Pallet not found.");

        var description = $"Performed Blast Freezing on pallet {pallet.Barcode}.";

        var vasTransaction = VASTransaction.Create(
            pallet.AccountId,
            pallet.Id,
            ServiceType.Blasting,
            command.UserId,
            description);

        foreach (var line in pallet.Lines)
        {
            vasTransaction.AddInputLine(line.MaterialId, line.Quantity, line.Weight, line.BatchNumber, line.ExpiryDate);
        }

        vasTransaction.Complete();
        await vasRepository.AddAsync(vasTransaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
