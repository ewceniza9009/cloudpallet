using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Companies.Queries;   
using WMS.Domain.ValueObjects;

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record UpdateWarehouseCommand(
    Guid Id,
    string Name,
    AddressDto Address,
    string OperatingHours,
    string ContactPhone,
    string ContactEmail,
    bool IsActive) : IRequest;

public class UpdateWarehouseCommandHandler(
    IWarehouseAdminRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateWarehouseCommand>
{
    public async Task Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await repository.GetByIdAsync(request.Id, cancellationToken, true)
            ?? throw new KeyNotFoundException($"Warehouse with ID {request.Id} not found.");

        var address = new Address(request.Address.Street, request.Address.City, request.Address.State, request.Address.PostalCode, request.Address.Country);

        warehouse.Update(
            request.Name,
            address,
            request.ContactPhone,
            request.ContactEmail,
            request.OperatingHours,
            request.IsActive
        );

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}