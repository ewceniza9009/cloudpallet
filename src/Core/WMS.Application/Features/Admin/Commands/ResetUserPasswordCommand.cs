using MediatR;
using Microsoft.AspNetCore.Identity;
using WMS.Application.Abstractions.Persistence;
using WMS.Domain.Entities;

namespace WMS.Application.Features.Admin.Commands;

public record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest;

public class ResetUserPasswordCommandHandler(
    UserManager<User> userManager,
    IUserRepository userRepository) : IRequestHandler<ResetUserPasswordCommand>
{
    public async Task Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User with ID {request.UserId} not found.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
        {
            throw new Exception($"Failed to reset password: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
