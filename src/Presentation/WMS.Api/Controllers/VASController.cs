using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Abstractions.Persistence;
using WMS.Application.Features.Inventory.Commands;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/vas")]
[Authorize(Roles = "Admin")]
public class VASController(
    IMediator mediator,
    IVASTransactionRepository vasRepository,
    IVASTransactionAmendmentRepository amendmentRepository) : ControllerBase
{
    [HttpGet("accounts/{accountId}/transactions")]
    public async Task<IActionResult> GetAccountTransactions(
        Guid accountId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] bool includeVoided = false,
        CancellationToken cancellationToken = default)
    {
        var transactions = await vasRepository.GetForAccountByPeriodAsync(
            accountId, startDate, endDate, cancellationToken);

        if (!includeVoided)
        {
            transactions = transactions.Where(t => !t.IsVoided);
        }

        var dtos = transactions.Select(t => new
        {
            t.Id,
            ServiceType = t.ServiceType.ToString(),
            t.Timestamp,
            t.Description,
            t.UserId,
            UserName = t.User?.UserName ?? "Unknown",
            IsAmended = t.GetAllLines().Any(l => l.IsAmended),
            t.IsVoided,
            t.VoidedAt,
            t.VoidReason
        });

        return Ok(dtos);
    }

    [HttpGet("transactions/{transactionId}")]
    public async Task<IActionResult> GetTransactionDetails(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await vasRepository.GetByIdWithLinesAndAmendmentsAsync(
            transactionId, cancellationToken);

        if (transaction == null)
            return NotFound($"Transaction {transactionId} not found");

        var dto = new
        {
            transaction.Id,
            ServiceType = transaction.ServiceType.ToString(),
            transaction.Timestamp,
            transaction.Description,
            transaction.Status,
            transaction.IsVoided,
            transaction.VoidedAt,
            transaction.VoidReason,
            InputLines = transaction.InputLines.Select(l => new
            {
                l.Id,
                l.MaterialId,
                MaterialName = l.Material?.Name,
                l.Quantity,
                l.Weight,
                l.IsInput,
                l.IsAmended,
                l.OriginalQuantity,
                l.OriginalWeight,
                l.AmendedAt
            }),
            OutputLines = transaction.OutputLines.Select(l => new
            {
                l.Id,
                l.MaterialId,
                MaterialName = l.Material?.Name,
                l.Quantity,
                l.Weight,
                l.IsInput,
                l.IsAmended,
                l.OriginalQuantity,
                l.OriginalWeight,
                l.AmendedAt
            }),
            AmendmentHistory = transaction.Amendments.Select(a => new
            {
                a.Id,
                a.Timestamp,
                UserName = a.User?.UserName ?? "Unknown",
                a.Reason,
                a.AmendmentDetails,
                AmendmentType = a.AmendmentType.ToString()
            })
        };

        return Ok(dto);
    }

    [HttpPost("transactions/{transactionId}/lines/{lineId}/amend")]
    public async Task<IActionResult> AmendTransactionLine(
        Guid transactionId,
        Guid lineId,
        [FromBody] AmendVasTransactionLineCommand command,
        CancellationToken cancellationToken = default)
    {
        // Override IDs from route
        command = command with
        {
            VasTransactionId = transactionId,
            VasTransactionLineId = lineId
        };

        var result = await mediator.Send(command, cancellationToken);
        return Ok(new { success = result });
    }

    [HttpPost("transactions/{transactionId}/void")]
    public async Task<IActionResult> VoidTransaction(
        Guid transactionId,
        [FromBody] VoidVasTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        // Override ID from route
        command = command with { VasTransactionId = transactionId };

        var result = await mediator.Send(command, cancellationToken);
        return Ok(new { success = result });
    }

    [HttpGet("transactions/{transactionId}/amendments")]
    public async Task<IActionResult> GetTransactionAmendments(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var amendments = await amendmentRepository.GetByTransactionIdAsync(
            transactionId, cancellationToken);

        var dtos = amendments.Select(a => new
        {
            a.Id,
            a.Timestamp,
            UserName = a.User?.UserName ?? "Unknown",
            a.Reason,
            a.AmendmentDetails,
            AmendmentType = a.AmendmentType.ToString()
        });

        return Ok(dtos);
    }
}
