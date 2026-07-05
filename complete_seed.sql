SET QUOTED_IDENTIFIER ON;
USE HOWA_Db;
GO

-- Insert approved attendees (UserId 3,4,5 = john_doe, jane_smith, sam_wilson)
INSERT INTO [dbo].[Attendees] ([UserId],[RfidUid],[QrCodeData],[Status],[ApprovedBy],[ApprovedAt])
VALUES
    (3, 'RFID_987654321', 'HOWA_john_doe_QR_TOKEN',   'Approved', 1, GETDATE()),
    (4, 'RFID_876543210', 'HOWA_jane_smith_QR_TOKEN',  'Approved', 1, GETDATE()),
    (5, 'RFID_765432109', 'HOWA_sam_wilson_QR_TOKEN',  'Approved', 2, GETDATE());
GO

-- Now insert attendance logs using the correct AttendeeIds
-- (john=1st approved insert → id depends on IDENTITY, check first)
DECLARE @john  INT = (SELECT AttendeeId FROM Attendees WHERE UserId = 3);
DECLARE @jane  INT = (SELECT AttendeeId FROM Attendees WHERE UserId = 4);
DECLARE @sam   INT = (SELECT AttendeeId FROM Attendees WHERE UserId = 5);

INSERT INTO [dbo].[AttendanceLogs] ([AttendeeId],[EventId],[Timestamp],[Method],[Status])
VALUES
    (@john, 1, DATEADD(minute,  5, DATEADD(day,-2,GETDATE())), 'RFID', 'Present'),
    (@jane, 1, DATEADD(minute, 12, DATEADD(day,-2,GETDATE())), 'QR',   'Present'),
    (@sam,  1, DATEADD(minute, 20, DATEADD(day,-2,GETDATE())), 'RFID', 'Late'),
    (@john, 2, DATEADD(minute,  3, DATEADD(day,-1,GETDATE())), 'RFID', 'Present'),
    (@jane, 2, DATEADD(minute,  8, DATEADD(day,-1,GETDATE())), 'QR',   'Present');
GO

-- Final verification
SELECT 'Users'          AS [Table], COUNT(*) AS [Rows] FROM [dbo].[Users]
UNION ALL SELECT 'Attendees',      COUNT(*) FROM [dbo].[Attendees]
UNION ALL SELECT 'Events',         COUNT(*) FROM [dbo].[Events]
UNION ALL SELECT 'AttendanceLogs', COUNT(*) FROM [dbo].[AttendanceLogs];

SELECT AttendeeId, UserId, RfidUid, Status FROM [dbo].[Attendees] ORDER BY AttendeeId;
GO
