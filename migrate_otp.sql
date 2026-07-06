SET QUOTED_IDENTIFIER ON;
USE HOWA_Db;
GO

-- ============================================================
-- Migration: Add OTP support
-- ============================================================

-- 1. OtpTokens table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OtpTokens' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[OtpTokens] (
        [OtpId]      INT          IDENTITY(1,1) NOT NULL,
        [AttendeeId] INT          NOT NULL,
        [EventId]    INT          NOT NULL,
        [Code]       NVARCHAR(6)  NOT NULL,
        [Method]     NVARCHAR(20) NOT NULL,
        [IssuedAt]   DATETIME     NOT NULL CONSTRAINT [DF_OtpTokens_IssuedAt] DEFAULT GETDATE(),
        [ExpiresAt]  DATETIME     NOT NULL,
        [IsUsed]     BIT          NOT NULL CONSTRAINT [DF_OtpTokens_IsUsed]   DEFAULT 0,
        CONSTRAINT [PK_OtpTokens]           PRIMARY KEY CLUSTERED ([OtpId] ASC),
        CONSTRAINT [FK_OtpTokens_Attendees] FOREIGN KEY ([AttendeeId]) REFERENCES [dbo].[Attendees]([AttendeeId]) ON DELETE CASCADE,
        CONSTRAINT [FK_OtpTokens_Events]    FOREIGN KEY ([EventId])    REFERENCES [dbo].[Events]([EventId])       ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_OtpTokens_ExpiresAt] ON [dbo].[OtpTokens]([ExpiresAt] ASC);
    PRINT 'OtpTokens table created.';
END
ELSE
    PRINT 'OtpTokens table already exists. Skipping.';
GO

-- 2. sp_IssueOtp
IF OBJECT_ID('dbo.sp_IssueOtp', 'P') IS NOT NULL DROP PROCEDURE [dbo].[sp_IssueOtp];
GO
CREATE PROCEDURE [dbo].[sp_IssueOtp]
    @ScanValue  NVARCHAR(255),
    @EventId    INT,
    @Method     NVARCHAR(20),
    @OtpId      INT         OUTPUT,
    @Code       NVARCHAR(6) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AttendeeId     INT          = NULL;
    DECLARE @AttendeeStatus NVARCHAR(20) = NULL;

    -- Resolve attendee by RFID or QR
    IF @Method = 'RFID'
        SELECT @AttendeeId = [AttendeeId], @AttendeeStatus = [Status]
        FROM [dbo].[Attendees] WHERE [RfidUid] = @ScanValue;
    ELSE IF @Method = 'QR'
        SELECT @AttendeeId = [AttendeeId], @AttendeeStatus = [Status]
        FROM [dbo].[Attendees] WHERE [QrCodeData] = @ScanValue;

    IF @AttendeeId IS NULL
    BEGIN
        RAISERROR('Attendee not found for the provided scan value.', 16, 1);
        RETURN;
    END

    IF @AttendeeStatus <> 'Approved'
    BEGIN
        RAISERROR('Attendee is not approved. Current status: %s.', 16, 1, @AttendeeStatus);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Events] WHERE [EventId] = @EventId)
    BEGIN
        RAISERROR('Event not found.', 16, 1);
        RETURN;
    END

    -- Expire any existing unused OTPs for this attendee + event
    UPDATE [dbo].[OtpTokens]
    SET    [IsUsed] = 1
    WHERE  [AttendeeId] = @AttendeeId
      AND  [EventId]    = @EventId
      AND  [IsUsed]     = 0;

    -- Generate 6-digit code
    SET @Code = RIGHT('000000' + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS NVARCHAR(6)), 6);

    -- Insert new OTP — expires in 5 minutes
    INSERT INTO [dbo].[OtpTokens] ([AttendeeId],[EventId],[Code],[Method],[IssuedAt],[ExpiresAt],[IsUsed])
    VALUES (@AttendeeId, @EventId, @Code, @Method, GETDATE(), DATEADD(minute, 5, GETDATE()), 0);

    SET @OtpId = SCOPE_IDENTITY();
END;
GO

-- 3. sp_VerifyOtp
IF OBJECT_ID('dbo.sp_VerifyOtp', 'P') IS NOT NULL DROP PROCEDURE [dbo].[sp_VerifyOtp];
GO
CREATE PROCEDURE [dbo].[sp_VerifyOtp]
    @OtpId   INT,
    @Code    NVARCHAR(6),
    @Success BIT           OUTPUT,
    @LogId   INT           OUTPUT,
    @Status  NVARCHAR(20)  OUTPUT,
    @Message NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    SET @LogId   = 0;
    SET @Status  = '';
    SET @Message = '';

    DECLARE @AttendeeId INT, @EventId INT, @StoredCode NVARCHAR(6),
            @ExpiresAt  DATETIME, @IsUsed BIT, @Method NVARCHAR(20);

    SELECT @AttendeeId = [AttendeeId], @EventId    = [EventId],
           @StoredCode = [Code],       @ExpiresAt  = [ExpiresAt],
           @IsUsed     = [IsUsed],     @Method     = [Method]
    FROM   [dbo].[OtpTokens]
    WHERE  [OtpId] = @OtpId;

    -- Validate OTP exists
    IF @AttendeeId IS NULL
    BEGIN
        SET @Message = 'Invalid OTP reference.';
        RETURN;
    END

    -- Already used
    IF @IsUsed = 1
    BEGIN
        SET @Message = 'OTP has already been used.';
        RETURN;
    END

    -- Expired
    IF GETDATE() > @ExpiresAt
    BEGIN
        UPDATE [dbo].[OtpTokens] SET [IsUsed] = 1 WHERE [OtpId] = @OtpId;
        SET @Message = 'OTP has expired. Please scan again.';
        RETURN;
    END

    -- Wrong code
    IF @StoredCode <> @Code
    BEGIN
        SET @Message = 'Incorrect OTP code. Please try again.';
        RETURN;
    END

    -- Mark OTP as consumed
    UPDATE [dbo].[OtpTokens] SET [IsUsed] = 1 WHERE [OtpId] = @OtpId;

    -- Already checked in for this event?
    IF EXISTS (SELECT 1 FROM [dbo].[AttendanceLogs]
               WHERE [AttendeeId] = @AttendeeId AND [EventId] = @EventId)
    BEGIN
        SELECT @LogId = [LogId], @Status = [Status]
        FROM   [dbo].[AttendanceLogs]
        WHERE  [AttendeeId] = @AttendeeId AND [EventId] = @EventId;
        SET @Success = 1;
        SET @Message = 'Already checked in for this event.';
        RETURN;
    END

    -- Determine Present vs Late (15-minute grace window)
    DECLARE @EventDate DATETIME;
    SELECT @EventDate = [EventDate] FROM [dbo].[Events] WHERE [EventId] = @EventId;

    SET @Status = CASE
        WHEN GETDATE() <= DATEADD(minute, 15, @EventDate) THEN 'Present'
        ELSE 'Late'
    END;

    -- Log the attendance
    INSERT INTO [dbo].[AttendanceLogs] ([AttendeeId],[EventId],[Timestamp],[Method],[Status])
    VALUES (@AttendeeId, @EventId, GETDATE(), @Method, @Status);

    SET @LogId   = SCOPE_IDENTITY();
    SET @Success = 1;
    SET @Message = 'Check-in confirmed. Status: ' + @Status;
END;
GO

PRINT 'OTP migration complete.';
GO
