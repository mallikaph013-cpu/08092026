-- Make AssignedItUserId and AssignedItName nullable in RepairTickets table
-- Required so a Rejected ticket can have its IT assignment cleared to NULL
-- (Status == Rejected clears all approver/assignment fields and resets Step to 1).

-- Update AssignedItUserId column to allow NULL
ALTER TABLE [RepairTickets] ALTER COLUMN [AssignedItUserId] NVARCHAR(450) NULL;

-- Update AssignedItName column to allow NULL
ALTER TABLE [RepairTickets] ALTER COLUMN [AssignedItName] NVARCHAR(150) NULL;

PRINT 'Updated AssignedItUserId and AssignedItName columns to allow NULL values';