using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Security;

namespace WMS.Application.Features.Admin.Commands;

public record UpdateUserStatusCommand(Guid UserId, bool IsActive) : IRequest;

public class UpdateUserStatusCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserStatusCommand>
{
    public async Task Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {request.UserId} not found.");

        user.SetStatus(request.IsActive);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
