using WMS.Application.Features.Companies.Queries;

namespace WMS.Application.Features.WarehouseSetup.Queries;

public record WarehouseDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    AddressDto Address,
    string OperatingHours,
    string ContactPhone,
    string ContactEmail,
    bool IsActive);