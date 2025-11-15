// ---- File: src/Core/WMS.Application/Features/Admin/Commands/DeleteRateCommand.cs ----

using MediatR;
using WMS.Application.Abstractions.Persistence;

namespace WMS.Application.Features.Admin.Commands;

public record DeleteRateCommand(Guid Id) : IRequest;

public class DeleteRateCommandHandler(
    IRateRepository rateRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteRateCommand>
{
    public async Task Handle(DeleteRateCommand request, CancellationToken cancellationToken)
    {
        var rate = await rateRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Rate with ID {request.Id} not found.");

        rateRepository.Remove(rate);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}