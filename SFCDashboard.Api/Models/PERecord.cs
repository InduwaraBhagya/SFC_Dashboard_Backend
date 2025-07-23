using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SFCDashboard.Models
{
    [Table("PERecords")]
    public class PERecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int ID { get; set; }

        [Column("PROVINCE")]
        public string? PROVINCE { get; set; }

        [Column("REGION")]
        public string? REGION { get; set; }

        [Column("RTOM")]
        public string? RTOM { get; set; }

        [Column("RTOM_DESCRIPTION")]
        public string? RTOM_DESCRIPTION { get; set; }

        [Column("JOB_REFERENCE")]
        public string? JOB_REFERENCE { get; set; }

        [Column("CONTRACTOR_NAME")]
        public string? CONTRACTOR_NAME { get; set; }

        [Column("PE_NUMBER")]
        public string? PE_NUMBER { get; set; }

        [Column("PE_ACTIVITY")]
        public string? PE_ACTIVITY { get; set; }

        [Column("PE_NATURE")]
        public string? PE_NATURE { get; set; }

        [Column("PE_TITLE")]
        public string? PE_TITLE { get; set; }

        [Column("PE_OBJECTIVE")]
        public string? PE_OBJECTIVE { get; set; }

        [Column("PE_AREA")]
        public string? PE_AREA { get; set; }

        [Column("SO_NUMBER")]
        public string? SO_NUMBER { get; set; }

        [Column("TASK_SEQ")]
        public int? TASK_SEQ { get; set; }

        [Column("TASK_NAME")]
        public string? TASK_NAME { get; set; }

        [Column("TASK_WG")]
        public string? TASK_WG { get; set; }

        [Column("WO_ACTUAL_START_DATE")]
        public string? WO_ACTUAL_START_DATE { get; set; }

        [Column("REQUEST_REFERENCE_NO")]
        public string? REQUEST_REFERENCE_NO { get; set; }

        [Column("SO_ID")]
        public string? SO_ID { get; set; }

        [Column("REGION_1")]
        public string? REGION_1 { get; set; }

        [Column("PROVINCE_1")]
        public string? PROVINCE_1 { get; set; }

        [Column("RTOM_1")]
        public string? RTOM_1 { get; set; }

        [Column("LEA")]
        public string? LEA { get; set; }

        [Column("CCT_ID")]
        public string? CCT_ID { get; set; }

        [Column("SERVICE_CATEGORY")]
        public string? SERVICE_CATEGORY { get; set; }

        [Column("SERVICE_TYPE")]
        public string? SERVICE_TYPE { get; set; }

        [Column("SO_CREATE_DATE")]
        public DateTime? SO_CREATE_DATE { get; set; }

        [Column("ORDER_TYPE")]
        public string? ORDER_TYPE { get; set; }

        [Column("CRM_ORDER")]
        public string? CRM_ORDER { get; set; }

        [Column("WO_ID")]
        public string? WO_ID { get; set; }

        [Column("PENDING_TASK_NAME")]
        public string? PENDING_TASK_NAME { get; set; }

        [Column("PENDING_WG")]
        public string? PENDING_WG { get; set; }

        [Column("WO_STATUS")]
        public string? WO_STATUS { get; set; }

        [Column("WO_START_DATE")]
        public DateTime? WO_START_DATE { get; set; }

        [Column("SERVICE_SPEED")]
        public string? SERVICE_SPEED { get; set; }

        [Column("SERVICE_REQUIRED_DATE")]
        public DateTime? SERVICE_REQUIRED_DATE { get; set; }

        [Column("FIBER_PE_NO")]
        public string? FIBER_PE_NO { get; set; }

        [Column("FIBER_SO_ID")]
        public string? FIBER_SO_ID { get; set; }

        [Column("PRODUCT_SO_ID")]
        public string? PRODUCT_SO_ID { get; set; }

        [Column("FIBER_PE_TASK_NAME")]
        public string? FIBER_PE_TASK_NAME { get; set; }

        [Column("FIBER_PE_TASK_WG")]
        public string? FIBER_PE_TASK_WG { get; set; }

        [Column("PE_WO_COMMENTS")]
        public string? PE_WO_COMMENTS { get; set; }

        [Column("CUSTOMER")]
        public string? CUSTOMER { get; set; }

        [Column("CUS_TYPE")]
        public string? CUS_TYPE { get; set; }

        [Column("ACCOUNT_MANAGER")]
        public string? ACCOUNT_MANAGER { get; set; }

        [Column("SECTION_HANDLED_BY")]
        public string? SECTION_HANDLED_BY { get; set; }

        [Column("LOCATION_A_ADDRESS")]
        public string? LOCATION_A_ADDRESS { get; set; }

        [Column("LOCATION_B_ADDRESS")]
        public string? LOCATION_B_ADDRESS { get; set; }

        [Column("NTU_TYPE")]
        public string? NTU_TYPE { get; set; }

        [Column("ACCESS_MEDIUM")]
        public string? ACCESS_MEDIUM { get; set; }

        [Column("ACCESS_MEDIUM_A_END")]
        public string? ACCESS_MEDIUM_A_END { get; set; }

        [Column("ACCESS_MEDIUM_B_END")]
        public string? ACCESS_MEDIUM_B_END { get; set; }

        [Column("WO_COMMENTS")]
        public string? WO_COMMENTS { get; set; }
    }
}
