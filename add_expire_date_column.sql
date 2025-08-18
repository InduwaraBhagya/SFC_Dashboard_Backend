-- SQL script to add ExpireDate column to the Notices table
-- Run this script on your database to add the new column

-- Add the ExpireDate column to the Notices table
ALTER TABLE Notices 
ADD ExpireDate DATETIME NULL;

-- Optional: Add a comment to describe the column
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Date when the notice expires and should no longer be displayed. NULL means no expiry date.', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Notices', 
    @level2type = N'COLUMN', @level2name = N'ExpireDate';

-- Optional: Create an index on ExpireDate for better query performance
CREATE NONCLUSTERED INDEX IX_Notices_ExpireDate_IsActive
ON Notices (ExpireDate, IsActive)
INCLUDE (IsPinned, CreatedDate);

-- Verification query to check the column was added
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Notices' 
    AND COLUMN_NAME = 'ExpireDate';

-- Sample query to test the new functionality
-- This query shows how the application will filter notices
SELECT 
    ID,
    Description,
    CreatedDate,
    IsPinned,
    ExpireDate,
    IsActive,
    CASE 
        WHEN ExpireDate IS NULL THEN 'No Expiry'
        WHEN ExpireDate >= CAST(GETDATE() AS DATE) THEN 'Active'
        ELSE 'Expired'
    END AS Status
FROM Notices
WHERE IsActive = 1 
    AND (ExpireDate IS NULL OR ExpireDate >= CAST(GETDATE() AS DATE))
ORDER BY IsPinned DESC, CreatedDate DESC;
