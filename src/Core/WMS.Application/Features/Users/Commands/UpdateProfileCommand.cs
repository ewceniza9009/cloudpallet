using MediatR;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Abstractions.Security;

namespace WMS.Application.Features.Users.Commands;

public record UpdateProfileCommand(string FirstName, string LastName) : IRequest;

public class UpdateProfileCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProfileCommand>
{
    public async Task Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        user.UpdateProfile(request.FirstName, request.LastName);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
