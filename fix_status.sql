SET QUOTED_IDENTIFIER ON;
USE HOWA_Db;
GO

UPDATE [dbo].[Attendees]
SET    [Status]     = 'Approved',
       [ApprovedAt] = GETDATE()
WHERE  [AttendeeId] = 14;
GO

SELECT [AttendeeId], [Status], [RfidUid], [ApprovedAt]
FROM   [dbo].[Attendees]
WHERE  [AttendeeId] = 14;
GO
