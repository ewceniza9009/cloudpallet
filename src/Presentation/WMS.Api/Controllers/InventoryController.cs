using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Api.Common;
using WMS.Application.Features.Inventory.Commands;
using WMS.Application.Features.Inventory.Queries;

namespace WMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "OperatorPolicy")]
public class InventoryController : ApiControllerBase
{
    [HttpGet("putaway-tasks")]
    [ProducesResponseType(typeof(IEnumerable<PutawayTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPutawayTasks([FromQuery] Guid warehouseId)
    {
        var result = await Mediator.Send(new GetPutawayTasksQuery(warehouseId));
        return Ok(result);
    }

    [HttpPost("record-vas")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordVas(RecordVasCommand command)
    {
        var commandWithUser = command with { UserId = User.GetUserId() };
        await Mediator.Send(commandWithUser);
        return NoContent();
    }

    [HttpGet("stored-pallets-by-room")]
    [ProducesResponseType(typeof(IEnumerable<RoomWithPalletsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStoredPalletsByRoom([FromQuery] Guid warehouseId)
    {
        var result = await Mediator.Send(new GetStoredPalletsByRoomQuery(warehouseId));
        return Ok(result);
    }

    [HttpGet("search-stored-pallets")]
    [ProducesResponseType(typeof(IEnumerable<StoredPalletSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchStoredPallets(
        [FromQuery] Guid warehouseId,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? materialId,
        [FromQuery] string? barcodeQuery)
    {
        var query = new SearchStoredPalletsQuery(warehouseId, accountId, materialId, barcodeQuery);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("pallet-history/{palletBarcode}")]
    [ProducesResponseType(typeof(IEnumerable<PalletMovementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPalletHistory(string palletBarcode)
    {
        var result = await Mediator.Send(new GetPalletHistoryQuery(palletBarcode));
        return Ok(result);
    }

    [HttpPost("transfer-pallet")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> TransferPallet(TransferPalletCommand command)
    {
        var commandWithUser = command with { UserId = User.GetUserId() };
        var result = await Mediator.Send(commandWithUser);
        return Ok(result);
    }

    [HttpPost("transfer-items")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> TransferItemsToNewPallet([FromBody] TransferItemsToNewPalletCommand command)
    {
        var commandWithUser = command with { UserId = User.GetUserId() };
        var newPalletId = await Mediator.Send(commandWithUser);
        return Ok(newPalletId);
    }

    [HttpPost("manual-putaway")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> ManualPutaway(ManualPutawayCommand command)
    {
        var commandWithUser = command with { UserId = User.GetUserId() };
        var result = await Mediator.Send(commandWithUser);

        return Ok(result);
    }

    [HttpPost("start-quarantine")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartQuarantine(StartQuarantineCommand command)
    {
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPost("create-kit")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateKit(CreateKitCommand command)
    {
        var commandWithUser = command with { UserId = User.GetUserId() };
        var newInventoryId = await Mediator.Send(commandWithUser);
        return Ok(newInventoryId);
    }
}