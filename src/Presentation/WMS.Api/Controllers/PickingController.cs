using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Api.Common;
using WMS.Application.Features.Shipments.Commands;
using WMS.Application.Features.Shipments.Queries;
using WMS.Domain.Enums;

namespace WMS.Api.Controllers;

// DTOs defined within the controller for clarity
public record ConfirmPickRequest(Guid PickTransactionId, PickStatus NewStatus);
public record ConfirmPickByScanRequest(Guid PickTransactionId, string ScannedLocationCode, string ScannedLpn, decimal ActualWeight);
public record OrderItemDto(Guid MaterialId, decimal Quantity);

// --- MODIFIED REQUEST DTO ---
public record CreatePickListRequest(
    List<OrderItemDto> OrderItems,
    bool IsExpedited // <-- ADDED PROPERTY HERE
    );
// --- END MODIFICATION ---

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "OperatorPolicy")]
public class PickingController : ApiControllerBase
{
    [HttpPost("create-list")]
    [ProducesResponseType(typeof(IEnumerable<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePickList(CreatePickListRequest request) // Request now includes IsExpedited
    {
        // Convert List<OrderItemDto> to Dictionary<Guid, decimal>
        var orderItemsDictionary = request.OrderItems.ToDictionary(item => item.MaterialId, item => item.Quantity);

        // --- MODIFIED COMMAND INSTANTIATION ---
        var command = new CreatePickListCommand(
            orderItemsDictionary,
            User.GetUserId(), // Get user ID from claims
            request.IsExpedited // <-- PASS THE FLAG HERE
        );
        // --- END MODIFICATION ---

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("lists")]
    [ProducesResponseType(typeof(IEnumerable<PickListGroupDto>), StatusCodes.Status200OK)] // Corrected DTO type
    public async Task<IActionResult> GetPickListForCurrentUser()
    {
        var currentUserId = User.GetUserId();
        var result = await Mediator.Send(new GetPickListQuery(currentUserId));
        return Ok(result);
    }

    [HttpPost("confirm-manual")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmPickManually(ConfirmPickRequest request)
    {
        var command = new ConfirmPickCommand(
            request.PickTransactionId,
            request.NewStatus,
            User.GetUserId());

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPost("confirm-by-scan")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPickByScan(ConfirmPickByScanRequest request)
    {
        var command = new ConfirmPickByScanCommand(
            request.PickTransactionId,
            request.ScannedLocationCode,
            request.ScannedLpn,
            request.ActualWeight,
            User.GetUserId());

        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("items/{pickTransactionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePickItem(Guid pickTransactionId)
    {
        var command = new DeletePickItemCommand(pickTransactionId);
        await Mediator.Send(command);
        return NoContent();
    }
}