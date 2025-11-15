-- Creates the procedure to query the inventory ledger entries for reporting
-- NO 'GO' STATEMENTS IN THIS FILE

CREATE PROCEDURE dbo.sp_GetInventoryLedger
    -- Filter Parameters
    @StartDate DATETIME2,
    @EndDate DATETIME2,
    @AccountId UNIQUEIDENTIFIER = NULL,    -- Optional filter
    @MaterialId UNIQUEIDENTIFIER = NULL,   -- Optional filter
    @SupplierId UNIQUEIDENTIFIER = NULL,   -- Optional filter
    @TruckId UNIQUEIDENTIFIER = NULL,      -- Optional filter
    @UserId UNIQUEIDENTIFIER = NULL         -- Optional filter
AS
BEGIN
    SET NOCOUNT ON;
    SET @EndDate = DATEADD(day, 1, CAST(@EndDate AS DATE));

    -- Select the raw ledger entries based on filters
    SELECT
        Timestamp,
        TransactionType,
        DocumentReference,
        MaterialId,
        MaterialName,
        AccountId,
        AccountName,
        -- Calculate IN/OUT columns for easier report display
        CASE WHEN QuantityChange >= 0 THEN QuantityChange ELSE 0 END AS QuantityIn,
        CASE WHEN QuantityChange < 0 THEN -QuantityChange ELSE 0 END AS QuantityOut, -- Make positive for display
        CASE WHEN WeightChange >= 0 THEN WeightChange ELSE 0 END AS WeightIn,
        CASE WHEN WeightChange < 0 THEN -WeightChange ELSE 0 END AS WeightOut,     -- Make positive for display
        QuantityChange, -- Return the raw +/- change needed for balance calculation in C#
        WeightChange,   -- Return the raw +/- change needed for balance calculation in C#
        LocationId,
        PalletId,
        SupplierId,
        TruckId,
        UserId,
        UserName
    FROM
        dbo.InventoryLedgerEntries -- Query the single, optimized ledger table
    WHERE
        Timestamp >= @StartDate AND Timestamp < @EndDate
        AND (@AccountId IS NULL OR AccountId = @AccountId)
        AND (@MaterialId IS NULL OR MaterialId = @MaterialId)
        AND (@SupplierId IS NULL OR SupplierId = @SupplierId)
        AND (@TruckId IS NULL OR TruckId = @TruckId)
        AND (@UserId IS NULL OR UserId = @UserId)
    ORDER BY
        -- Order is crucial for calculating running balance later in C#
        MaterialName, -- Group results visually by material
        Timestamp;    -- Ensure chronological order within each material

END;