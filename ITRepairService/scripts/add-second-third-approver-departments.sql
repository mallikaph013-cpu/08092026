-- Add fields for SecondApproverDepartment and ThirdApproverDepartment
-- These fields store the department of the second and third approver in the approval workflow

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RepairTickets') AND name = 'SecondApproverDepartment')
BEGIN
    ALTER TABLE RepairTickets
    ADD SecondApproverDepartment NVARCHAR(100) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RepairTickets') AND name = 'ThirdApproverDepartment')
BEGIN
    ALTER TABLE RepairTickets
    ADD ThirdApproverDepartment NVARCHAR(100) NULL;
END

PRINT 'SecondApproverDepartment and ThirdApproverDepartment fields added successfully';