using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record UpdateCarrierCommand(
    Guid Id,
    string Name,
    string ScacCode,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    Address? Address,
    bool CertificationColdChain,
    string? InsurancePolicyNumber,
    DateTime? InsuranceExpiryDate,
    bool IsActive) : IRequest;

public class UpdateCarrierCommandHandler(
    ICarrierRepository carrierRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCarrierCommand>
{
    public async Task Handle(UpdateCarrierCommand request, CancellationToken cancellationToken)
    {
        var carrier = await carrierRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Carrier with ID {request.Id} not found.");

        if (request.Address != null)
        {
            carrier.UpdateContactInfo(request.ContactName, request.ContactPhone, request.ContactEmail, request.Address);
        }

        if (!string.IsNullOrEmpty(request.InsurancePolicyNumber))
        {
            carrier.SetInsuranceDetails(request.InsurancePolicyNumber, request.InsuranceExpiryDate ?? DateTime.UtcNow.AddYears(1));
        }

        if (request.IsActive) carrier.Activate(); else carrier.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}