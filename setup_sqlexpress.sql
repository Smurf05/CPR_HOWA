USE HOWA_Db;
GO

-- Attendees Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Attendees')
CREATE TABLE [dbo].[Attendees] (
    [AttendeeId] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [RfidUid] NVARCHAR(50) NULL,
    [QrCodeData] NVARCHAR(255) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    [ApprovedBy] INT NULL,
    [ApprovedAt] DATETIME NULL,
    CONSTRAINT [PK_Attendees] PRIMARY KEY CLUSTERED ([AttendeeId] ASC),
    CONSTRAINT [FK_Attendees_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Attendees_Users_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [dbo].[Users]([UserId]),
    CONSTRAINT [UQ_Attendees_RfidUid] UNIQUE ([RfidUid]),
    CONSTRAINT [UQ_Attendees_QrCodeData] UNIQUE ([QrCodeData])
);
CREATE NONCLUSTERED INDEX [IX_Attendees_Status] ON [dbo].[Attendees]([Status] ASC);
GO

-- Events Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Events')
CREATE TABLE [dbo].[Events] (
    [EventId] INT IDENTITY(1,1) NOT NULL,
    [EventName] NVARCHAR(150) NOT NULL,
    [EventDate] DATETIME NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED ([EventId] ASC)
);
GO

-- AttendanceLogs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AttendanceLogs')
CREATE TABLE [dbo].[AttendanceLogs] (
    [LogId] INT IDENTITY(1,1) NOT NULL,
    [AttendeeId] INT NOT NULL,
    [EventId] INT NOT NULL,
    [Timestamp] DATETIME NOT NULL DEFAULT GETDATE(),
    [Method] NVARCHAR(20) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL,
    CONSTRAINT [PK_AttendanceLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
    CONSTRAINT [FK_AttendanceLogs_Attendees] FOREIGN KEY ([AttendeeId]) REFERENCES [dbo].[Attendees]([AttendeeId]) ON DELETE CASCADE,
    CONSTRAINT [FK_AttendanceLogs_Events] FOREIGN KEY ([EventId]) REFERENCES [dbo].[Events]([EventId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Attendance_Attendee_Event] UNIQUE ([AttendeeId], [EventId])
);
CREATE NONCLUSTERED INDEX [IX_AttendanceLogs_Timestamp] ON [dbo].[AttendanceLogs]([Timestamp] ASC);
GO

-- Views
IF OBJECT_ID('dbo.vw_AttendeeDetails','V') IS NOT NULL DROP VIEW [dbo].[vw_AttendeeDetails];
GO
CREATE VIEW [dbo].[vw_AttendeeDetails] AS
SELECT a.[AttendeeId], u.[UserId], u.[Username], u.[FirstName], u.[LastName],
       u.[Email], u.[ContactNo], a.[RfidUid], a.[QrCodeData],
       a.[Status] AS [AttendeeStatus], a.[ApprovedAt],
       u_app.[Username] AS [ApprovedByUsername]
FROM [dbo].[Attendees] a
INNER JOIN [dbo].[Users] u ON a.[UserId] = u.[UserId]
LEFT JOIN [dbo].[Users] u_app ON a.[ApprovedBy] = u_app.[UserId];
GO

IF OBJECT_ID('dbo.vw_AttendanceReport','V') IS NOT NULL DROP VIEW [dbo].[vw_AttendanceReport];
GO
CREATE VIEW [dbo].[vw_AttendanceReport] AS
SELECT l.[LogId], a.[AttendeeId], u.[UserId], u.[FirstName], u.[LastName],
       u.[Role] AS [UserRole], e.[EventId], e.[EventName], e.[EventDate],
       l.[Timestamp] AS [ScannedAt], l.[Method] AS [AttendanceMethod], l.[Status] AS [AttendanceStatus]
FROM [dbo].[AttendanceLogs] l
INNER JOIN [dbo].[Attendees] a ON l.[AttendeeId] = a.[AttendeeId]
INNER JOIN [dbo].[Users] u ON a.[UserId] = u.[UserId]
INNER JOIN [dbo].[Events] e ON l.[EventId] = e.[EventId];
GO

-- Stored Procedures
IF OBJECT_ID('dbo.sp_RegisterUser','P') IS NOT NULL DROP PROCEDURE [dbo].[sp_RegisterUser];
GO
CREATE PROCEDURE [dbo].[sp_RegisterUser]
    @Username NVARCHAR(50), @Password NVARCHAR(255),
    @FirstName NVARCHAR(50), @LastName NVARCHAR(50),
    @Email NVARCHAR(100), @ContactNo NVARCHAR(20),
    @QrCodeData NVARCHAR(255),
    @NewUserId INT OUTPUT, @NewAttendeeId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO [dbo].[Users] ([Username],[Password],[Role],[FirstName],[LastName],[Email],[ContactNo])
        VALUES (@Username,@Password,'Attendee',@FirstName,@LastName,@Email,@ContactNo);
        SET @NewUserId = SCOPE_IDENTITY();
        INSERT INTO [dbo].[Attendees] ([UserId],[QrCodeData],[Status]) VALUES (@NewUserId,@QrCodeData,'Pending');
        SET @NewAttendeeId = SCOPE_IDENTITY();
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH ROLLBACK TRANSACTION; THROW; END CATCH
END;
GO

IF OBJECT_ID('dbo.sp_ApproveAttendee','P') IS NOT NULL DROP PROCEDURE [dbo].[sp_ApproveAttendee];
GO
CREATE PROCEDURE [dbo].[sp_ApproveAttendee]
    @AttendeeId INT, @RfidUid NVARCHAR(50), @ApprovedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Attendees]
    SET [Status]='Approved', [RfidUid]=NULLIF(TRIM(@RfidUid),''), [ApprovedBy]=@ApprovedBy, [ApprovedAt]=GETDATE()
    WHERE [AttendeeId]=@AttendeeId;
END;
GO

IF OBJECT_ID('dbo.sp_LogAttendance','P') IS NOT NULL DROP PROCEDURE [dbo].[sp_LogAttendance];
GO
CREATE PROCEDURE [dbo].[sp_LogAttendance]
    @ScanValue NVARCHAR(255), @EventId INT, @Method NVARCHAR(20),
    @LogId INT OUTPUT, @Status NVARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AttendeeId INT = NULL, @AttendeeStatus NVARCHAR(20) = NULL;
    IF @Method='RFID' SELECT @AttendeeId=[AttendeeId],@AttendeeStatus=[Status] FROM [dbo].[Attendees] WHERE [RfidUid]=@ScanValue;
    ELSE IF @Method='QR' SELECT @AttendeeId=[AttendeeId],@AttendeeStatus=[Status] FROM [dbo].[Attendees] WHERE [QrCodeData]=@ScanValue;
    IF @AttendeeId IS NULL BEGIN RAISERROR('Attendee not found.',16,1); RETURN; END
    IF @AttendeeStatus<>'Approved' BEGIN RAISERROR('Attendee not approved.',16,1); RETURN; END
    DECLARE @EventDate DATETIME;
    SELECT @EventDate=[EventDate] FROM [dbo].[Events] WHERE [EventId]=@EventId;
    IF @EventDate IS NULL BEGIN RAISERROR('Event not found.',16,1); RETURN; END
    SET @Status = CASE WHEN GETDATE() <= DATEADD(minute,15,@EventDate) THEN 'Present' ELSE 'Late' END;
    IF EXISTS (SELECT 1 FROM [dbo].[AttendanceLogs] WHERE [AttendeeId]=@AttendeeId AND [EventId]=@EventId)
    BEGIN
        SELECT @LogId=[LogId],@Status=[Status] FROM [dbo].[AttendanceLogs] WHERE [AttendeeId]=@AttendeeId AND [EventId]=@EventId;
        RETURN;
    END
    INSERT INTO [dbo].[AttendanceLogs] ([AttendeeId],[EventId],[Timestamp],[Method],[Status])
    VALUES (@AttendeeId,@EventId,GETDATE(),@Method,@Status);
    SET @LogId = SCOPE_IDENTITY();
END;
GO

IF OBJECT_ID('dbo.sp_GetDashboardStats','P') IS NOT NULL DROP PROCEDURE [dbo].[sp_GetDashboardStats];
GO
CREATE PROCEDURE [dbo].[sp_GetDashboardStats] @EventId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TotalApproved INT, @TotalPresent INT, @PendingApprovals INT, @AttendanceRate DECIMAL(5,2)=0.00;
    SELECT @TotalApproved=COUNT(*) FROM [dbo].[Attendees] WHERE [Status]='Approved';
    SELECT @TotalPresent=COUNT(*) FROM [dbo].[AttendanceLogs] WHERE [EventId]=@EventId;
    SELECT @PendingApprovals=COUNT(*) FROM [dbo].[Attendees] WHERE [Status]='Pending';
    IF @TotalApproved>0 SET @AttendanceRate=CAST((@TotalPresent*100.0)/@TotalApproved AS DECIMAL(5,2));
    SELECT ISNULL(@TotalApproved,0) AS [TotalRegistered], ISNULL(@TotalPresent,0) AS [TotalPresent],
           ISNULL(@PendingApprovals,0) AS [PendingApprovals], ISNULL(@AttendanceRate,0.00) AS [AttendanceRate];
END;
GO

-- Seed Data
INSERT INTO [dbo].[Users] ([Username],[Password],[Role],[FirstName],[LastName],[Email],[ContactNo])
VALUES
    ('admin1','Password123','Admin','Aldrin Jone','Bisin','aldrin@scc.edu.ph','09123456789'),
    ('admin2','Password123','Admin','Zoe','Majani','zoe@scc.edu.ph','09987654321'),
    ('john_doe','Password123','Attendee','John','Doe','johndoe@scc.edu.ph','09151234567'),
    ('jane_smith','Password123','Attendee','Jane','Smith','janesmith@scc.edu.ph','09267654321'),
    ('sam_wilson','Password123','Attendee','Sam','Wilson','samw@scc.edu.ph','09379876543'),
    ('alice_jones','Password123','Attendee','Alice','Jones','alicej@scc.edu.ph','09452345678'),
    ('bob_brown','Password123','Attendee','Bob','Brown','bobb@scc.edu.ph','09568765432');
GO

INSERT INTO [dbo].[Attendees] ([UserId],[RfidUid],[QrCodeData],[Status],[ApprovedBy],[ApprovedAt])
VALUES
    (3,'RFID_987654321','HOWA_john_doe_QR_TOKEN','Approved',1,GETDATE()),
    (4,'RFID_876543210','HOWA_jane_smith_QR_TOKEN','Approved',1,GETDATE()),
    (5,'RFID_765432109','HOWA_sam_wilson_QR_TOKEN','Approved',2,GETDATE()),
    (6,NULL,'HOWA_alice_jones_QR_TOKEN','Pending',NULL,NULL),
    (7,NULL,'HOWA_bob_brown_QR_TOKEN','Pending',NULL,NULL);
GO

INSERT INTO [dbo].[Events] ([EventName],[EventDate],[Description])
VALUES
    ('Midweek Chapel Convocation',DATEADD(day,-2,GETDATE()),'SCC Midweek religious convocation.'),
    ('Friday Praise and Worship',DATEADD(day,-1,GETDATE()),'Weekend spiritual gathering.'),
    ('Sunday Service Convocation',DATEADD(hour,2,GETDATE()),'Main Sunday religious service.');
GO

INSERT INTO [dbo].[AttendanceLogs] ([AttendeeId],[EventId],[Timestamp],[Method],[Status])
VALUES
    (1,1,DATEADD(minute,5,DATEADD(day,-2,GETDATE())),'RFID','Present'),
    (2,1,DATEADD(minute,12,DATEADD(day,-2,GETDATE())),'QR','Present'),
    (3,1,DATEADD(minute,20,DATEADD(day,-2,GETDATE())),'RFID','Late'),
    (1,2,DATEADD(minute,3,DATEADD(day,-1,GETDATE())),'RFID','Present'),
    (2,2,DATEADD(minute,8,DATEADD(day,-1,GETDATE())),'QR','Present');
GO

PRINT 'HOWA_Db setup complete on SQLEXPRESS.';
