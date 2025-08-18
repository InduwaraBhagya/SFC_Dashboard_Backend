-- SQL script to reset ExpireDate column in the Notices table
-- This script will drop the existing column and recreate it properly

-- First, check if the ExpireDate column exists and drop it if it does
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Notices' 
    AND COLUMN_NAME = 'ExpireDate'
)
BEGIN
    PRINT 'ExpireDate column exists. Dropping existing column...'
    
    -- Drop any existing index on ExpireDate column
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notices_ExpireDate_IsActive')
    BEGIN
        DROP INDEX IX_Notices_ExpireDate_IsActive ON Notices;
        PRINT 'Dropped existing index IX_Notices_ExpireDate_IsActive'
    END
    
    -- Drop the ExpireDate column
    ALTER TABLE Notices DROP COLUMN ExpireDate;
    PRINT 'ExpireDate column dropped successfully.'
END
ELSE
BEGIN
    PRINT 'ExpireDate column does not exist. Proceeding to create it...'
END

-- Add the ExpireDate column with proper data type
ALTER TABLE Notices 
ADD ExpireDate DATETIME NULL;

PRINT 'ExpireDate column added successfully.'

-- Add a description for the column
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Date and time when the notice expires and should no longer be displayed. NULL means no expiry date.', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Notices', 
    @level2type = N'COLUMN', @level2name = N'ExpireDate';

PRINT 'Added column description.'

-- Create an optimized index for better query performance
CREATE NONCLUSTERED INDEX IX_Notices_ExpireDate_IsActive
ON Notices (ExpireDate, IsActive)
INCLUDE (IsPinned, CreatedDate, Description, CreatedUserName);

PRINT 'Created optimized index IX_Notices_ExpireDate_IsActive.'

-- Verification query to confirm the column structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Notices' 
AND COLUMN_NAME = 'ExpireDate';

PRINT 'ExpireDate column setup completed successfully!'

-- Sample query to test the new functionality
-- This shows how the application will filter notices
SELECT 
    ID,
    Description,
    CreatedDate,
    IsPinned,
    ExpireDate,
    IsActive,
    CASE 
        WHEN ExpireDate IS NULL THEN 'No Expiry'
        WHEN ExpireDate >= GETDATE() THEN 'Active'
        ELSE 'Expired'
    END AS Status
FROM Notices
WHERE IsActive = 1 
    AND (ExpireDate IS NULL OR ExpireDate >= GETDATE())
ORDER BY IsPinned DESC, CreatedDate DESC;

PRINT 'Sample query executed successfully. Notice filtering is working correctly!'
