using MediatR;
using Microsoft.AspNetCore.Identity;
using WMS.Application.Abstractions.Security;
using WMS.Domain.Entities;

namespace WMS.Application.Features.Users.Commands;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public class ChangePasswordCommandHandler(
    ICurrentUserService currentUserService,
    UserManager<User> userManager) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            throw new Exception($"Failed to change password: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
