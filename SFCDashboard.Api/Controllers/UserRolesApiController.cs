using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SFCDashboard.Data;
using SFCDashboard.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFCDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/userroles")]
    [Produces("application/json")]
    public class UserRolesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRolesApiController> _logger;

        public UserRolesApiController(ApplicationDbContext context, ILogger<UserRolesApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/userroles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserRole>>> GetUserRoles()
        {
            var roles = await _context.UserRoles.Include(r => r.RolePermissions).ToListAsync();
            return Ok(roles);
        }

        // GET: api/userroles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserRole>> GetUserRole(int id)
        {
            var role = await _context.UserRoles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
                return NotFound();
            return Ok(role);
        }

        // POST: api/userroles
        [HttpPost]
        public async Task<ActionResult<UserRole>> CreateUserRole([FromBody] UserRole userRole)
        {
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUserRole), new { id = userRole.Id }, userRole);
        }

        // PUT: api/userroles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UserRole userRole)
        {
            if (id != userRole.Id)
                return BadRequest();
            _context.Entry(userRole).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserRoleExists(id))
                    return NotFound();
                else
                    throw;
            }
            return NoContent();
        }

        // DELETE: api/userroles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserRole(int id)
        {
            var role = await _context.UserRoles.FindAsync(id);
            if (role == null)
                return NotFound();
            _context.UserRoles.Remove(role);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/userroles/{id}/exists
        [HttpGet("{id}/exists")]
        public async Task<ActionResult<bool>> UserRoleExistsApi(int id)
        {
            return await _context.UserRoles.AnyAsync(e => e.Id == id);
        }

        // GET: api/userroles/{id}/with-permissions
        [HttpGet("{id}/with-permissions")]
        public async Task<ActionResult<UserRole>> GetUserRoleWithPermissions(int id)
        {
            var role = await _context.UserRoles.Include(r => r.RolePermissions)
                                               .ThenInclude(rp => rp.Permission)
                                               .FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
                return NotFound();
            return Ok(role);
        }

        // GET: api/userroles/{id}/with-rolepermissions
        [HttpGet("{id}/with-rolepermissions")]
        public async Task<ActionResult<UserRole>> GetUserRoleWithRolePermissions(int id)
        {
            var role = await _context.UserRoles.Include(r => r.RolePermissions)
                                               .ThenInclude(rp => rp.Permission)
                                               .FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
                return NotFound();
            return Ok(role);
        }

        private bool UserRoleExists(int id)
        {
            return _context.UserRoles.Any(e => e.Id == id);
        }
    }
}
