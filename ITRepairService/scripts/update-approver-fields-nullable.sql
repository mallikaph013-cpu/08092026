-- Make ApproverUserId and ApproverName nullable in RepairTickets table
-- This allows tickets to be created without an approver (before approval)

-- Update ApproverUserId column to allow NULL
ALTER TABLE [RepairTickets] ALTER COLUMN [ApproverUserId] NVARCHAR(450) NULL;

-- Update ApproverName column to allow NULL
ALTER TABLE [RepairTickets] ALTER COLUMN [ApproverName] NVARCHAR(150) NULL;

-- Remove the default constraints (optional, since NULL is now allowed)
-- Note: SQL Server doesn't allow removing default constraints directly in a simple way
-- The defaults will be ignored once NULL is allowed

PRINT 'Updated ApproverUserId and ApproverName columns to allow NULL values';