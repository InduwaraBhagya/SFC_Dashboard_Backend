-- 1. Sync missing PlannedEvents from soms_mig1.so_record to SFCDB.PlannedEvents
-- Using SO_ID as the PE_NUMBER to ensure uniqueness and avoid constraint violations
WITH UniqueSOs AS (
    SELECT sor.*,
           ROW_NUMBER() OVER (PARTITION BY sor.SO_ID ORDER BY sor.SO_CREATE_DATE DESC) as rn
    FROM soms_mig1.dbo.so_record sor
    WHERE sor.SO_ID IN (SELECT DISTINCT SO_ID FROM soms_mig1.dbo.ProjectSOMappings)
)
INSERT INTO SFCDB.dbo.PlannedEvents (
    SO_ID, PE_NUMBER, PE_CREATED_DATE, IsHold,
    REGION, PROVINCE, RTOM, LEA, CCT_ID, 
    SERVICE_CATEGORY, SERVICE_TYPE, SO_CREATE_DATE, ORDER_TYPE, 
    CRM_ORDER, WO_ID, PENDING_TASK_NAME, PENDING_WG, 
    WO_STATUS, WO_START_DATE, SERVICE_SPEED, SERVICE_REQUIRED_DATE, 
    FIBER_PE_NO, FIBER_SO_ID, PRODUCT_SO_ID, FIBER_PE_TASK_NAME, 
    FIBER_PE_TASK_WG, PE_WO_COMMENTS, CUSTOMER, CUS_TYPE, 
    ACCOUNT_MANAGER, SECTION_HANDLED_BY, LOCATION_A_ADDRESS, 
    LOCATION_B_ADDRESS, NTU_TYPE, ACCESS_MEDIUM, ACCESS_MEDIUM_A_END, 
    ACCESS_MEDIUM_B_END, WO_COMMENTS, PE_STATUS
)
SELECT 
    sor.SO_ID, 
    sor.SO_ID as PE_NUMBER, -- Guaranteed unique
    ISNULL(sor.SO_CREATE_DATE, GETDATE()) as PE_CREATED_DATE, 
    0 as IsHold,
    sor.REGION, sor.PROVINCE, sor.RTOM, sor.LEA, sor.CCT_ID, 
    sor.SERVICE_CATEGORY, sor.SERVICE_TYPE, sor.SO_CREATE_DATE, sor.ORDER_TYPE, 
    sor.CRM_ORDER, sor.WO_ID, sor.PENDING_TASK_NAME, sor.PENDING_WG, 
    sor.WO_STATUS, sor.WO_START_DATE, sor.SERVICE_SPEED, sor.SERVICE_REQUIRED_DATE, 
    sor.FIBER_PE_NO, sor.FIBER_SO_ID, sor.PRODUCT_SO_ID, sor.FIBER_PE_TASK_NAME, 
    sor.FIBER_PE_TASK_WG, sor.PE_WO_COMMENTS, sor.CUSTOMER, sor.CUS_TYPE, 
    sor.ACCOUNT_MANAGER, sor.SECTION_HANDLED_BY, sor.LOCATION_A_ADDRESS, 
    sor.LOCATION_B_ADDRESS, sor.NTU_TYPE, sor.ACCESS_MEDIUM, sor.ACCESS_MEDIUM_A_END, 
    sor.ACCESS_MEDIUM_B_END, sor.WO_COMMENTS, sor.SO_STATUS
FROM UniqueSOs sor
WHERE rn = 1
AND NOT EXISTS (SELECT 1 FROM SFCDB.dbo.PlannedEvents pe WHERE pe.SO_ID = sor.SO_ID)
AND NOT EXISTS (SELECT 1 FROM SFCDB.dbo.PlannedEvents pe WHERE pe.PE_NUMBER = sor.SO_ID);

-- 2. Sync Mappings
DELETE FROM SFCDB.dbo.ProjectPEMappings;
DELETE FROM SFCDB.dbo.Projects;

DBCC CHECKIDENT ('SFCDB.dbo.Projects', RESEED, 0);
DBCC CHECKIDENT ('SFCDB.dbo.ProjectPEMappings', RESEED, 0);

INSERT INTO SFCDB.dbo.Projects (ProjectName, CreatedDate)
SELECT ProjectName, CreatedDate
FROM soms_mig1.dbo.Projects;

INSERT INTO SFCDB.dbo.ProjectPEMappings (ProjectId, PlannedEventId)
SELECT 
    newP.Id as ProjectId,
    pe.Id as PlannedEventId
FROM soms_mig1.dbo.ProjectSOMappings oldM
JOIN soms_mig1.dbo.Projects oldP ON oldM.ProjectId = oldP.Id
JOIN SFCDB.dbo.Projects newP ON newP.ProjectName = oldP.ProjectName
JOIN SFCDB.dbo.PlannedEvents pe ON pe.SO_ID = oldM.SO_ID;

-- Results
SELECT 'Projects: ' + CAST(COUNT(*) AS VARCHAR) FROM SFCDB.dbo.Projects;
SELECT 'Mappings: ' + CAST(COUNT(*) AS VARCHAR) FROM SFCDB.dbo.ProjectPEMappings;
