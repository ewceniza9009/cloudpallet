-- Creates the procedure to insert a single ledger entry (Uses RAISERROR for compatibility)
-- NO 'GO' STATEMENTS IN THIS FILE

CREATE PROCEDURE dbo.sp_InsertInventoryLedgerEntry
    -- Core Info
    @Timestamp DATETIME2,
    @MaterialId UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @TransactionType NVARCHAR(30), -- Pass enum string value from C#
    @TransactionId UNIQUEIDENTIFIER,
    -- Changes
    @QuantityChange DECIMAL(18, 5), -- Positive=IN, Negative=OUT
    @WeightChange DECIMAL(18, 5),   -- Positive=IN, Negative=OUT
    -- Denormalized Data
    @MaterialName NVARCHAR(150),
    @AccountName NVARCHAR(150),
    @DocumentReference NVARCHAR(100),
    @UserName NVARCHAR(100) = NULL, -- Optional user name
    -- Optional Contextual IDs
    @LocationId UNIQUEIDENTIFIER = NULL,
    @PalletId UNIQUEIDENTIFIER = NULL,
    @SupplierId UNIQUEIDENTIFIER = NULL,
    @TruckId UNIQUEIDENTIFIER = NULL,
    @UserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    -- Prevents sending 'rows affected' messages back to the client
    SET NOCOUNT ON;

    -- Basic Validation using RAISERROR (Severity 16 = User Error, State 1 = Standard)
    IF @MaterialId = '00000000-0000-0000-0000-000000000000' OR @MaterialId IS NULL OR LTRIM(RTRIM(ISNULL(@MaterialName,''))) = '' BEGIN
        RAISERROR('MaterialId and MaterialName are required.', 16, 1);
        RETURN; -- Stop execution
    END
    IF @AccountId = '00000000-0000-0000-0000-000000000000' OR @AccountId IS NULL OR LTRIM(RTRIM(ISNULL(@AccountName,''))) = '' BEGIN
        RAISERROR('AccountId and AccountName are required.', 16, 1);
        RETURN;
    END
     IF @TransactionId = '00000000-0000-0000-0000-000000000000' OR @TransactionId IS NULL OR LTRIM(RTRIM(ISNULL(@TransactionType,''))) = '' BEGIN
        RAISERROR('TransactionId and TransactionType are required.', 16, 1);
        RETURN;
    END
     IF LTRIM(RTRIM(ISNULL(@DocumentReference,''))) = '' BEGIN
        RAISERROR('DocumentReference is required.', 16, 1);
        RETURN;
    END

    -- Insert the record
    INSERT INTO dbo.InventoryLedgerEntries (
        Id, Timestamp, MaterialId, AccountId, TransactionType, TransactionId,
        QuantityChange, WeightChange, MaterialName, AccountName, DocumentReference,
        LocationId, PalletId, SupplierId, TruckId, UserId, UserName
    )
    VALUES (
        NEWID(), @Timestamp, @MaterialId, @AccountId, @TransactionType, @TransactionId,
        @QuantityChange, @WeightChange, @MaterialName, @AccountName, @DocumentReference,
        @LocationId, @PalletId, @SupplierId, @TruckId, @UserId, @UserName
    );
END;