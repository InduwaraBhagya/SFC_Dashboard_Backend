using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Api.Data;
using System.Data.Common;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/somsdashboard")]
    public class SomsDashboardApiController : ControllerBase
    {
        private readonly SomsDbContext _context;

        public SomsDashboardApiController(SomsDbContext context)
        {
            _context = context;
        }

        [HttpGet("metrics/{workgroupName}")]
        public async Task<IActionResult> GetDashboardMetrics(string workgroupName)
        {
            int urgentCount = 0;
            int inProgressCount = 0;
            int olaViolatedCount = 0;
            int holdCount = 0;
            int dormantCount = 0;

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        (SELECT COUNT(*) FROM so_record s JOIN urgent_records u ON s.SO_ID = u.SO_ID WHERE s.PENDING_WG = @wg AND u.urgentStatus = 'approved' AND s.WO_STATUS != 'COMPLETED') as UrgentCount,
                        (SELECT COUNT(*) FROM so_record WHERE PENDING_WG = @wg AND WO_STATUS = 'INPROGRESS') as InProgressCount,
                        (SELECT COUNT(*) FROM so_record WHERE PENDING_WG = @wg AND (OLA_Reminder = 'Violated' OR PENDING_TASK_NAME = 'OLAViolated')) as OlaViolatedCount,
                        (SELECT COUNT(*) FROM so_record WHERE PENDING_WG = @wg AND WO_STATUS IN ('HOLD', 'PENDING_HOLD')) as HoldCount,
                        (SELECT COUNT(*) FROM so_record WHERE PENDING_WG = @wg AND WO_STATUS = 'DORMANT') as DormantCount
                ";
                var param = command.CreateParameter();
                param.ParameterName = "@wg";
                param.Value = workgroupName;
                command.Parameters.Add(param);

                _context.Database.OpenConnection();
                using (var result = await command.ExecuteReaderAsync())
                {
                    if (await result.ReadAsync())
                    {
                        urgentCount = result.IsDBNull(0) ? 0 : result.GetInt32(0);
                        inProgressCount = result.IsDBNull(1) ? 0 : result.GetInt32(1);
                        olaViolatedCount = result.IsDBNull(2) ? 0 : result.GetInt32(2);
                        holdCount = result.IsDBNull(3) ? 0 : result.GetInt32(3);
                        dormantCount = result.IsDBNull(4) ? 0 : result.GetInt32(4);
                    }
                }
                _context.Database.CloseConnection();
            }

            return Ok(new
            {
                urgent = urgentCount,
                inProgress = inProgressCount,
                olaViolated = olaViolatedCount,
                hold = holdCount,
                dormant = dormantCount,
                activeTeamMembers = 1, // Mock for now until mapped
                recentActivities = new { olaLeft = 0, totalTasks = inProgressCount }
            });
        }

        [HttpGet("records/{workgroupName}/{category}")]
        public async Task<IActionResult> GetDashboardRecords(string workgroupName, string category)
        {
            var records = new List<object>();

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                string commonFields = "SO_ID, CUSTOMER, SERVICE_TYPE, ORDER_TYPE, SERVICE_REQUIRED_DATE, PENDING_TASK_NAME, PENDING_WG, SERVICE_CATEGORY, SO_CREATE_DATE, WO_ID, WO_STATUS, WO_COMMENTS, CUS_TYPE, ACCOUNT_MANAGER, REGION, PROVINCE, LOCATION_A_ADDRESS, WO_START_DATE, RTOM";
                string sql;
                switch (category.ToLower())
                {
                    case "urgent":
                        sql = $"SELECT s.SO_ID, s.CUSTOMER, s.SERVICE_TYPE, s.ORDER_TYPE, s.SERVICE_REQUIRED_DATE, s.PENDING_TASK_NAME, s.PENDING_WG, s.SERVICE_CATEGORY, s.SO_CREATE_DATE, s.WO_ID, s.WO_STATUS, s.WO_COMMENTS, s.CUS_TYPE, s.ACCOUNT_MANAGER, s.REGION, s.PROVINCE, s.LOCATION_A_ADDRESS, s.WO_START_DATE, s.RTOM FROM so_record s JOIN urgent_records u ON s.SO_ID = u.SO_ID WHERE s.PENDING_WG = @wg AND u.urgentStatus = 'approved' AND s.WO_STATUS != 'COMPLETED'";
                        break;
                    case "inprogress":
                        sql = $"SELECT {commonFields} FROM so_record WHERE PENDING_WG = @wg AND WO_STATUS = 'INPROGRESS'";
                        break;
                    case "olaviolated":
                        sql = $"SELECT {commonFields} FROM so_record WHERE PENDING_WG = @wg AND (OLA_Reminder = 'Violated' OR PENDING_TASK_NAME = 'OLAViolated')";
                        break;
                    case "hold":
                        sql = $"SELECT {commonFields} FROM so_record WHERE PENDING_WG = @wg AND WO_STATUS IN ('HOLD', 'PENDING_HOLD')";
                        break;
                    case "dormant":
                        sql = $"SELECT {commonFields} FROM so_record WHERE PENDING_WG = @wg AND WO_STATUS = 'DORMANT'";
                        break;
                    default:
                        return BadRequest("Invalid category");
                }

                command.CommandText = sql;
                var param = command.CreateParameter();
                param.ParameterName = "@wg";
                param.Value = workgroupName;
                command.Parameters.Add(param);

                _context.Database.OpenConnection();
                using (var result = await command.ExecuteReaderAsync())
                {
                    while (await result.ReadAsync())
                    {
                        Func<int, string?> getString = (index) => result.IsDBNull(index) ? null : result.GetValue(index).ToString();
                        Func<int, string?> getDateString = (index) => {
                            if (result.IsDBNull(index)) return null;
                            var val = result.GetValue(index);
                            if (val is DateTime dt) return dt.ToString("yyyy/MM/dd");
                            return val.ToString();
                        };

                        records.Add(new
                        {
                            soId = getString(0),
                            customer = getString(1),
                            serviceType = getString(2),
                            orderType = getString(3),
                            serviceCategory = getString(7),
                            soCreateDate = getDateString(8),
                            woId = getString(9),
                            woStatus = getString(10),
                            woActualStartDate = (string?)null,
                            cusType = getString(12),
                            accountManager = getString(13),
                            region = getString(14),
                            province = getString(15),
                            locationAAddress = getString(16),
                            woStartDate = getDateString(17),
                            rtom = getString(18),
                            pendingTaskName = getString(5),
                            pendingWg = getString(6),
                            woComments = getString(11),
                            plannedEvent = new 
                            {
                                serviceRequiredDate = getDateString(4),
                                pendingTaskName = getString(5),
                                pendingWg = getString(6),
                                woComments = getString(11)
                            }
                        });
                    }
                }
                _context.Database.CloseConnection();
            }

            return Ok(records);
        }
    }
}
