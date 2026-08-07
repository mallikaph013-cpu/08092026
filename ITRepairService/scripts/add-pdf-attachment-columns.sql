-- Add PDF attachment columns to RepairTickets table
-- For SQLite:
-- ALTER TABLE RepairTickets ADD COLUMN PdfAttachmentPath TEXT;
-- ALTER TABLE RepairTickets ADD COLUMN PdfAttachmentFileName TEXT;
-- ALTER TABLE RepairTickets ADD COLUMN PdfAttachmentFileSize INTEGER;
-- CREATE INDEX IX_RepairTickets_PdfAttachmentPath ON RepairTickets(PdfAttachmentPath);

-- For SQL Server:
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RepairTickets') AND name = 'PdfAttachmentPath')
BEGIN
    ALTER TABLE RepairTickets ADD PdfAttachmentPath NVARCHAR(500) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RepairTickets') AND name = 'PdfAttachmentFileName')
BEGIN
    ALTER TABLE RepairTickets ADD PdfAttachmentFileName NVARCHAR(255) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RepairTickets') AND name = 'PdfAttachmentFileSize')
BEGIN
    ALTER TABLE RepairTickets ADD PdfAttachmentFileSize BIGINT NULL;
END

-- Add index for better query performance (if not exists)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('RepairTickets') AND name = 'IX_RepairTickets_PdfAttachmentPath')
BEGIN
    CREATE INDEX IX_RepairTickets_PdfAttachmentPath ON RepairTickets(PdfAttachmentPath);
END
