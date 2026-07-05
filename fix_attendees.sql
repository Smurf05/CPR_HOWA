SET QUOTED_IDENTIFIER ON;
USE HOWA_Db;
GO

-- Fix the RfidUid unique constraint to allow multiple NULLs
ALTER TABLE [dbo].[Attendees] DROP CONSTRAINT [UQ_Attendees_RfidUid];
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_Attendees_RfidUid]
    ON [dbo].[Attendees]([RfidUid])
    WHERE [RfidUid] IS NOT NULL;
GO

-- Insert the two pending attendees
INSERT INTO [dbo].[Attendees] ([UserId],[RfidUid],[QrCodeData],[Status],[ApprovedBy],[ApprovedAt])
VALUES
    (6, NULL, 'HOWA_alice_jones_QR_TOKEN', 'Pending', NULL, NULL),
    (7, NULL, 'HOWA_bob_brown_QR_TOKEN',   'Pending', NULL, NULL);
GO

-- Insert attendance log seed data
INSERT INTO [dbo].[AttendanceLogs] ([AttendeeId],[EventId],[Timestamp],[Method],[Status])
VALUES
    (1, 1, DATEADD(minute,  5, DATEADD(day,-2,GETDATE())), 'RFID', 'Present'),
    (2, 1, DATEADD(minute, 12, DATEADD(day,-2,GETDATE())), 'QR',   'Present'),
    (3, 1, DATEADD(minute, 20, DATEADD(day,-2,GETDATE())), 'RFID', 'Late'),
    (1, 2, DATEADD(minute,  3, DATEADD(day,-1,GETDATE())), 'RFID', 'Present'),
    (2, 2, DATEADD(minute,  8, DATEADD(day,-1,GETDATE())), 'QR',   'Present');
GO

-- Verify
SELECT 'Users'          AS [Table], COUNT(*) AS [Rows] FROM [dbo].[Users]
UNION ALL
SELECT 'Attendees',       COUNT(*) FROM [dbo].[Attendees]
UNION ALL
SELECT 'Events',          COUNT(*) FROM [dbo].[Events]
UNION ALL
SELECT 'AttendanceLogs',  COUNT(*) FROM [dbo].[AttendanceLogs];
GO
