using MediatR;

namespace WMS.Application.Features.Yard.Commands;

public record VacateYardSpotCommand(Guid YardSpotId) : IRequest<Unit>;
