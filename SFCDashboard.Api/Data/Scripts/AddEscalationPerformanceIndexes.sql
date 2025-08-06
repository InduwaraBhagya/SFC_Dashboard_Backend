-- Performance indexes for Escalation queries
-- This script adds indexes to improve the performance of escalation queries that are timing out

-- 1. Index on Escalations table for the main query filter
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Escalations_Level_CreatedAt' AND object_id = OBJECT_ID('Escalations'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Escalations_Level_CreatedAt] 
    ON [dbo].[Escalations] ([Level], [CreatedAt] DESC)
    INCLUDE ([Id], [TaskId], [Title], [Message], [IsRead], [IsIgnored], [IgnoreReason], [IgnoredAt], [IgnoredById])
END

-- 2. Index on PETasks table for the join condition and workgroup filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PETasks_Id_TaskWorkGroup' AND object_id = OBJECT_ID('PETasks'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PETasks_Id_TaskWorkGroup] 
    ON [dbo].[PETasks] ([Id])
    INCLUDE ([PENumber], [TaskWorkGroup], [Task], [TaskStatus], [Priority], [IsUrgent], [OLA], [OLADateTime], [IsOLAViolate])
END

-- 2a. Additional index for workgroup filtering (if TaskWorkGroup is not too long)
-- This will help with the WHERE clause filtering on TaskWorkGroup
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PETasks_TaskWorkGroup_Id' AND object_id = OBJECT_ID('PETasks'))
BEGIN
    -- Try to create index with TaskWorkGroup as key column (will fail if column is too long)
    BEGIN TRY
        CREATE NONCLUSTERED INDEX [IX_PETasks_TaskWorkGroup_Id] 
        ON [dbo].[PETasks] ([TaskWorkGroup], [Id])
        INCLUDE ([PENumber])
        PRINT 'Created TaskWorkGroup key index successfully'
    END TRY
    BEGIN CATCH
        -- If TaskWorkGroup is too long for key column, create a filtered index instead
        PRINT 'TaskWorkGroup too long for key column, creating filtered index instead'
        -- Create multiple filtered indexes for common workgroups to improve performance
        -- You can add more as needed based on your most common workgroup values
        
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PETasks_Id_NET_PLAN_ACC' AND object_id = OBJECT_ID('PETasks'))
        BEGIN
            CREATE NONCLUSTERED INDEX [IX_PETasks_Id_NET_PLAN_ACC] 
            ON [dbo].[PETasks] ([Id])
            INCLUDE ([PENumber], [TaskWorkGroup])
            WHERE [TaskWorkGroup] = 'NET-PLAN-ACC'
        END
        
        -- Add more filtered indexes for other common workgroups as needed
        -- Example: WHERE [TaskWorkGroup] = 'ANOTHER-WORKGROUP'
    END CATCH
END

-- 3. Index on PlannedEvents table for the join condition
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PlannedEvents_PeNumber' AND object_id = OBJECT_ID('PlannedEvents'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PlannedEvents_PeNumber] 
    ON [dbo].[PlannedEvents] ([PE_NUMBER])
    INCLUDE ([Id], [PE_TITLE], [CUSTOMER], [PE_STATUS], [PRIORITY], [SERVICE_REQUIRED_DATE])
END

-- 4. Composite index for the most common query pattern
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Escalations_TaskId_Level_CreatedAt' AND object_id = OBJECT_ID('Escalations'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Escalations_TaskId_Level_CreatedAt] 
    ON [dbo].[Escalations] ([TaskId], [Level], [CreatedAt] DESC)
END

-- 5. Update statistics to help the query optimizer
UPDATE STATISTICS [dbo].[Escalations]
UPDATE STATISTICS [dbo].[PETasks]
UPDATE STATISTICS [dbo].[PlannedEvents]

PRINT 'Escalation performance indexes created successfully'
