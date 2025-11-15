using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record CreateCarrierCommand(
    string Name,
    string ScacCode,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    Address? Address,
    bool CertificationColdChain,
    string? InsurancePolicyNumber,
    DateTime? InsuranceExpiryDate) : IRequest<Guid>;

public class CreateCarrierCommandHandler(
    IWarehouseAdminRepository warehouseAdminRepository,       
    ICarrierRepository carrierRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCarrierCommand, Guid>
{
    public async Task<Guid> Handle(CreateCarrierCommand request, CancellationToken cancellationToken)
    {
        var carrier = Carrier.Create(request.Name, request.ScacCode);

        if (request.Address != null)
        {
            carrier.UpdateContactInfo(request.ContactName, request.ContactPhone, request.ContactEmail, request.Address);
        }
        if (!string.IsNullOrEmpty(request.InsurancePolicyNumber))
        {
            carrier.SetInsuranceDetails(request.InsurancePolicyNumber, request.InsuranceExpiryDate ?? DateTime.UtcNow.AddYears(1));
        }

        await carrierRepository.AddAsync(carrier, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return carrier.Id;
    }
}