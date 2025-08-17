-- Manual migration script for Notices table
-- Run this script if Entity Framework migrations are not set up

-- First check if the table already exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Notices')
BEGIN
    -- Create Notices table
    CREATE TABLE [dbo].[Notices] (
        [ID] INT IDENTITY(1,1) NOT NULL,
        [Description] NVARCHAR(1000) NOT NULL,
        [CreatedDate] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
        [CreatedBy] INT NOT NULL,
        [CreatedUserName] NVARCHAR(255) NOT NULL,
        [IsPinned] BIT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [UpdatedDate] DATETIME2(7) NULL,
        [UpdatedBy] INT NULL,
        [UpdatedUserName] NVARCHAR(255) NULL,
        
        CONSTRAINT [PK_Notices] PRIMARY KEY CLUSTERED ([ID] ASC)
    );

    -- Add indexes for better performance
    CREATE NONCLUSTERED INDEX [IX_Notices_IsActive] ON [dbo].[Notices] ([IsActive] ASC);
    CREATE NONCLUSTERED INDEX [IX_Notices_IsPinned] ON [dbo].[Notices] ([IsPinned] ASC);
    CREATE NONCLUSTERED INDEX [IX_Notices_CreatedDate] ON [dbo].[Notices] ([CreatedDate] DESC);
    CREATE NONCLUSTERED INDEX [IX_Notices_CreatedBy] ON [dbo].[Notices] ([CreatedBy] ASC);

    -- Add sample data
    INSERT INTO [dbo].[Notices] ([Description], [CreatedBy], [CreatedUserName], [IsPinned])
    VALUES 
        ('Welcome to the Notice Board! This is a sample notice that demonstrates the new notice board functionality.', 1, 'System Admin', 1),
        ('Please remember to update your project status regularly. This helps keep the team informed about progress.', 1, 'System Admin', 0),
        ('New features have been added to the dashboard. Check out the improved Reports section and notice board.', 1, 'System Admin', 0),
        ('Maintenance window scheduled for next weekend. All systems will be updated during this time.', 1, 'System Admin', 1);

    PRINT 'Notices table created successfully with sample data.';
END
ELSE
BEGIN
    PRINT 'Notices table already exists. Skipping creation.';
END

GO
