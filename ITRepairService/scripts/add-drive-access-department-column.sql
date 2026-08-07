-- Add DriveAccessDepartment column to RepairTickets table
ALTER TABLE RepairTickets 
ADD DriveAccessDepartment NVARCHAR(100) NULL;

-- Add index for better query performance
CREATE INDEX IX_RepairTickets_DriveAccessDepartment 
ON RepairTickets (DriveAccessDepartment);