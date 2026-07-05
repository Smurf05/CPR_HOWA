SET QUOTED_IDENTIFIER ON;
USE HOWA_Db;
GO

-- Insert pending attendees (RfidUid NULLs allowed via filtered index now)
INSERT INTO [dbo].[Attendees] ([UserId],[RfidUid],[QrCodeData],[Status],[ApprovedBy],[ApprovedAt])
VALUES
    (6, NULL, 'HOWA_alice_jones_QR_TOKEN', 'Pending', NULL, NULL),
    (7, NULL, 'HOWA_bob_brown_QR_TOKEN',   'Pending', NULL, NULL);
GO

-- Insert attendance logs (AttendeeIds 1-3 exist now)
INSERT INTO [dbo].[AttendanceLogs] ([AttendeeId],[EventId],[Timestamp],[Method],[Status])
VALUES
    (1, 1, DATEADD(minute,  5, DATEADD(day,-2,GETDATE())), 'RFID', 'Present'),
    (2, 1, DATEADD(minute, 12, DATEADD(day,-2,GETDATE())), 'QR',   'Present'),
    (3, 1, DATEADD(minute, 20, DATEADD(day,-2,GETDATE())), 'RFID', 'Late'),
    (1, 2, DATEADD(minute,  3, DATEADD(day,-1,GETDATE())), 'RFID', 'Present'),
    (2, 2, DATEADD(minute,  8, DATEADD(day,-1,GETDATE())), 'QR',   'Present');
GO

-- Final check
SELECT 'Users'          AS [Table], COUNT(*) AS [Rows] FROM [dbo].[Users]
UNION ALL SELECT 'Attendees',      COUNT(*) FROM [dbo].[Attendees]
UNION ALL SELECT 'Events',         COUNT(*) FROM [dbo].[Events]
UNION ALL SELECT 'AttendanceLogs', COUNT(*) FROM [dbo].[AttendanceLogs];
GO
