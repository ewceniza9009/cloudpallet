// ---- File: src/Core/WMS.Application/Features/Inventory/Commands/AddPalletToReceivingCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Integrations;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Security; // <-- ADDED
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Entities; // <-- ADDED
using WMS.Domain.Enums; // <-- ADDED

namespace WMS.Application.Features.Inventory.Commands;

public record AddPalletToReceivingCommand(
    Guid ReceivingId,
    Guid PalletTypeId,
    bool IsCrossDock
    ) : IRequest<Guid>
{
    // We add UserId in case we need it for the VAS transaction
    public Guid UserId { get; init; }
}


public class AddPalletToReceivingCommandHandler(
    IReceivingTransactionRepository receivingRepository,
    IPalletTypeRepository palletTypeRepository,
    IBarcodeGenerationService barcodeGenerationService,
    IVASTransactionRepository vasRepository, // <-- ADDED
    ICurrentUserService currentUserService, // <-- ADDED
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddPalletToReceivingCommand, Guid>
{
    public async Task<Guid> Handle(AddPalletToReceivingCommand request, CancellationToken cancellationToken)
    {
        var receiving = await receivingRepository.GetByIdAsync(request.ReceivingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Receiving session with ID {request.ReceivingId} not found.");

        if (!receiving.AccountId.HasValue)
        {
            throw new InvalidOperationException("Cannot add pallet: The parent receiving session does not have an AccountId.");
        }

        var palletType = await palletTypeRepository.GetByIdAsync(request.PalletTypeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pallet Type with ID {request.PalletTypeId} not found.");

        var palletCount = await receivingRepository.GetPalletCountAsync(request.ReceivingId, cancellationToken);
        var palletNumber = $"{palletCount + 1}";
        receiving.IncrementPalletCount();

        var newPallet = Pallet.Create(
            request.ReceivingId,
            request.PalletTypeId,
            palletNumber,
            palletType.TareWeight,
            receiving.AccountId.Value,
            request.IsCrossDock); // The IsCrossDock flag is still set on the pallet for operational logic

        var palletBarcode = barcodeGenerationService.GenerateSSCCBarcode(newPallet.Id);
        newPallet.SetBarcode(palletBarcode);

        await receivingRepository.AddPalletAsync(newPallet, cancellationToken);

        // --- START: ADDED VAS TRANSACTION ---
        if (request.IsCrossDock)
        {
            var userId = currentUserService.UserId ?? Guid.Empty; // Get current user
            var description = $"Cross-Docking service fee for Pallet {palletBarcode}.";

            var vasTransaction = VASTransaction.Create(
                receiving.AccountId.Value,
                newPallet.Id, // Link the service to the new pallet
                ServiceType.CrossDock,
                userId,
                description);

            // Add a single line item representing 1 pallet for billing
            // We use Guid.Empty for MaterialId as this is a pallet-level service
            vasTransaction.AddInputLine(null, 1, 0); ; // 1 Pallet, 0 Weight
            vasTransaction.Complete();

            await vasRepository.AddAsync(vasTransaction, cancellationToken);
        }
        // --- END: ADDED VAS TRANSACTION ---

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newPallet.Id;
    }
}