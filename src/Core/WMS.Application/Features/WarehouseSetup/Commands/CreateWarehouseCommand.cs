using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Companies.Queries;
using WMS.Domain.ValueObjects;
using WarehouseEntity = WMS.Domain.Aggregates.Warehouse.Warehouse;     

namespace WMS.Application.Features.WarehouseSetup.Commands;

public record CreateWarehouseCommand(
    string Name,
    AddressDto Address,
    string OperatingHours,
    string ContactPhone,
    string ContactEmail) : IRequest<Guid>;

public class CreateWarehouseCommandHandler(
    IWarehouseAdminRepository repository,
    ICompanyRepository companyRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateWarehouseCommand, Guid>
{
    private const string FIXED_COMPANY_ID = "10000000-0000-0000-0000-000000000001";

    public async Task<Guid> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetCompanyAsync(cancellationToken);
        var companyId = company?.Id ?? Guid.Parse(FIXED_COMPANY_ID);

        var address = new Address(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.PostalCode,
            request.Address.Country);

        var warehouse = WarehouseEntity.Create(companyId, request.Name, address);

        warehouse.Update(
            request.Name,
            address,
            request.ContactPhone,
            request.ContactEmail,
            request.OperatingHours,
            true
        );

        await repository.AddAsync(warehouse, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return warehouse.Id;
    }
}