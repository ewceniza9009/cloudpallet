using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Admin.Commands;

public record UpdateUserRoleCommand(Guid UserId, UserRole NewRole) : IRequest;

public class UpdateUserRoleCommandHandler(
    IUserRepository userRepository,      
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserRoleCommand>
{
    public async Task Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {request.UserId} not found.");

        user.ChangeRole(request.NewRole);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}