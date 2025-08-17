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
    
    CONSTRAINT [PK_Notices] PRIMARY KEY CLUSTERED ([ID] ASC),
    
    -- Add indexes for better performance
    INDEX [IX_Notices_IsActive] NONCLUSTERED ([IsActive] ASC),
    INDEX [IX_Notices_IsPinned] NONCLUSTERED ([IsPinned] ASC),
    INDEX [IX_Notices_CreatedDate] NONCLUSTERED ([CreatedDate] DESC),
    INDEX [IX_Notices_CreatedBy] NONCLUSTERED ([CreatedBy] ASC)
);

-- Add sample data (optional)
INSERT INTO [dbo].[Notices] ([Description], [CreatedBy], [CreatedUserName], [IsPinned])
VALUES 
    ('Welcome to the Notice Board! This is a sample notice.', 1, 'System Admin', 1),
    ('Please remember to update your project status regularly.', 1, 'System Admin', 0),
    ('New features have been added to the dashboard.', 1, 'System Admin', 0);

-- Add a default constraint for CreatedDate if not already done
ALTER TABLE [dbo].[Notices] 
ADD CONSTRAINT [DF_Notices_CreatedDate] DEFAULT (GETDATE()) FOR [CreatedDate];

GO
