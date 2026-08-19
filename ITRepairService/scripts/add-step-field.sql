-- Add Step field to RepairTickets table
-- This script adds a numeric Step field to track workflow step numbers (1-100)

-- Check if column already exists
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RepairTickets' 
    AND COLUMN_NAME = 'Step'
)
BEGIN
    -- Add the Step column with default value
    ALTER TABLE RepairTickets
    ADD Step INT NOT NULL CONSTRAINT DF_RepairTickets_Step DEFAULT 1;

    -- Add constraint to ensure Step is between 1 and 100
    ALTER TABLE RepairTickets
    ADD CONSTRAINT CHK_RepairTickets_Step CHECK (Step >= 1 AND Step <= 100);

    PRINT 'Step column added successfully';
END
ELSE
BEGIN
    PRINT 'Step column already exists - skipping';
END

-- Verification query (commented out to avoid errors)
-- Uncomment after running the ALTER TABLE to verify
-- SELECT 
--     COLUMN_NAME, 
--     DATA_TYPE, 
--     IS_NULLABLE, 
--     COLUMN_DEFAULT
-- FROM INFORMATION_SCHEMA.COLUMNS
-- WHERE TABLE_NAME = 'RepairTickets' 
--     AND COLUMN_NAME = 'Step';