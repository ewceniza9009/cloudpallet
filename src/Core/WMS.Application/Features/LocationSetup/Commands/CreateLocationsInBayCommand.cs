using MediatR;
using WMS.Domain.Enums;

namespace WMS.Application.Features.LocationSetup.Commands;

public record CreateLocationsInBayCommand(
    Guid RoomId,
    string Bay,
    int StartRow, int EndRow,
    int StartCol, int EndCol,
    int StartLevel, int EndLevel,
    LocationType ZoneType) : IRequest;    