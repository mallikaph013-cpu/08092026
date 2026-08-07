-- Add fields for next approver and next approving department
-- These fields track who should approve the ticket next in the workflow

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RepairTickets') AND name = 'NextApproverUserId')
BEGIN
    ALTER TABLE RepairTickets
    ADD NextApproverUserId NVARCHAR(450) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RepairTickets') AND name = 'NextApproverName')
BEGIN
    ALTER TABLE RepairTickets
    ADD NextApproverName NVARCHAR(150) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RepairTickets') AND name = 'NextApproverDepartment')
BEGIN
    ALTER TABLE RepairTickets
    ADD NextApproverDepartment NVARCHAR(100) NULL;
END

PRINT 'Next approver fields added successfully';