using WMS.Application.Common.Models;
using WMS.Domain.Enums;

namespace WMS.Application.Features.LocationSetup.Queries;

public record LocationDto(
    Guid Id,
    string Barcode,
    string Bay,
    int Row,
    int Column,
    int Level,
    string ZoneType,
    decimal CapacityWeight,
    bool IsEmpty,
    bool IsActive);

public record RoomDto(
    Guid Id,
    string Name,
    string ServiceType,
    decimal MinTemp,
    decimal MaxTemp,
    int LocationCount);

public record RoomDetailDto(
    Guid Id,
    Guid WarehouseId,
    string Name,
    string ServiceType,
    decimal MinTemp,
    decimal MaxTemp,
    bool IsActive);