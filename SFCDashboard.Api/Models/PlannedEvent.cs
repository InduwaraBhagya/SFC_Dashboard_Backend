using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Api.Models
{
  public class PlannedEvent
  {
    [Key]
    public int Id { get; set; }

    [Column("PROVINCE")]
    public string? Province { get; set; }

    [Column("REGION")]
    public string? Region { get; set; }

    [Column("RTOM")]
    public string? Rtom { get; set; }

    [Column("RTOM_DESCRIPTION")]
    public string? RtomDescription { get; set; }

    [Column("JOB_REFERENCE")]
    public string? JobReference { get; set; }

    [Column("CONTRACTOR_NAME")]
    public string? ContractorName { get; set; }

    [Column("PE_NUMBER")]
    public string? PeNumber { get; set; }

    [Column("PE_ACTIVITY")]
    public string? PeActivity { get; set; }

    [Column("PE_NATURE")]
    public string? PeNature { get; set; }

    [Column("PE_TITLE")]
    public string? PeTitle { get; set; }

    [Column("PE_OBJECTIVE")]
    public string? PeObjective { get; set; }

    [Column("PE_AREA")]
    public string? PeArea { get; set; }

    [Column("SO_NUMBER")]
    public string? SoNumber { get; set; }

    [Column("TASK_SEQ")]
    public int? TaskSeq { get; set; }

    [Column("TASK_NAME")]
    public string? TaskName { get; set; }

    [Column("TASK_WG")]
    public string? TaskWg { get; set; }

    [Column("WO_ACTUAL_START_DATE")]
    public string? WoActualStartDate { get; set; }

    [Column("REQUEST_REFERENCE_NO")]
    public string? RequestReferenceNo { get; set; }

    [Column("SO_ID")]
    public string? SoId { get; set; }

    [Column("REGION_1")]
    public string? Region1 { get; set; }

    [Column("PROVINCE_1")]
    public string? Province1 { get; set; }

    [Column("RTOM_1")]
    public string? Rtom1 { get; set; }

    [Column("LEA")]
    public string? Lea { get; set; }

    [Column("CCT_ID")]
    public string? CctId { get; set; }

    [Column("SERVICE_CATEGORY")]
    public string? ServiceCategory { get; set; }

    [Column("SERVICE_TYPE")]
    public string? ServiceType { get; set; }

    [Column("SO_CREATE_DATE")]
    public DateTime? SoCreateDate { get; set; }

    [Column("ORDER_TYPE")]
    public string? OrderType { get; set; }

    [Column("CRM_ORDER")]
    public string? CrmOrder { get; set; }

    [Column("WO_ID")]
    public string? WoId { get; set; }

    [Column("PENDING_TASK_NAME")]
    public string? PendingTaskName { get; set; }

    [Column("PENDING_WG")]
    public string? PendingWg { get; set; }

    [Column("WO_STATUS")]
    public string? WoStatus { get; set; }

    [Column("WO_START_DATE")]
    public DateTime? WoStartDate { get; set; }

    [Column("SERVICE_SPEED")]
    public string? ServiceSpeed { get; set; }

    [Column("SERVICE_REQUIRED_DATE")]
    public DateTime? ServiceRequiredDate { get; set; }

    [Column("FIBER_PE_NO")]
    public string? FiberPeNo { get; set; }

    [Column("FIBER_SO_ID")]
    public string? FiberSoId { get; set; }

    [Column("PRODUCT_SO_ID")]
    public string? ProductSoId { get; set; }

    [Column("FIBER_PE_TASK_NAME")]
    public string? FiberPeTaskName { get; set; }

    [Column("FIBER_PE_TASK_WG")]
    public string? FiberPeTaskWg { get; set; }

    [Column("PE_WO_COMMENTS")]
    public string? PeWoComments { get; set; }

    [Column("CUSTOMER")]
    public string? Customer { get; set; }

    [Column("CUS_TYPE")]
    public string? CusType { get; set; }

    [Column("ACCOUNT_MANAGER")]
    public string? AccountManager { get; set; }

    [Column("SECTION_HANDLED_BY")]
    public string? SectionHandledBy { get; set; }

    [Column("LOCATION_A_ADDRESS")]
    public string? LocationAAddress { get; set; }

    [Column("LOCATION_B_ADDRESS")]
    public string? LocationBAddress { get; set; }

    [Column("NTU_TYPE")]
    public string? NtuType { get; set; }

    [Column("ACCESS_MEDIUM")]
    public string? AccessMedium { get; set; }

    [Column("ACCESS_MEDIUM_A_END")]
    public string? AccessMediumAEnd { get; set; }

    [Column("ACCESS_MEDIUM_B_END")]
    public string? AccessMediumBEnd { get; set; }

    [Column("WO_COMMENTS")]
    public string? WoComments { get; set; }

    [Column("PE_STATUS")]
    public string? PEStatus { get; set; }
    [Column("PRIORITY")]
    public string? Priority { get; set; }

    [Column("PE_CREATED_DATE")]
    public DateTime PECreatedDate { get; set; }

    // Add this new property for tracking hold status
    public bool IsHold { get; set; } = false; // Default value is false

    public bool HasPendingUrgentRequest => PEStatus == "PENDING_URGENT_CONFIRMATION";
    [Column("CreatedDateFromPE")]
    public DateTime? CreatedDateFromPE { get; set; }
    public virtual ICollection<Escalation>? Escalations { get; set; }
    public string? UrgentRequestedByName { get; internal set; }
    public int? UrgentRequestedById { get; internal set; }
  }
}

