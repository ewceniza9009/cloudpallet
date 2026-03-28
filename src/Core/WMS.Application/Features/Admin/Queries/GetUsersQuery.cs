using WMS.Application.Common.Mappings;
using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Admin.Queries;

public record UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
}

public record GetUsersQuery : IRequest<IEnumerable<UserDto>>;

public class GetUsersQueryHandler(IUserRepository userRepository, IWmsMapper mapper) : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
{
    public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return mapper.MapToDtos(users);
    }
}