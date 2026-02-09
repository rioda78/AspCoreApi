using AspCoreApi.Data;
using AspCoreApi.Filters;
using AspCoreApi.Helpers;
using AspCoreApi.Models;
using AspCoreApi.Services;
using AspCoreApi.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AspCoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditLogService _auditLogService;

    public UsersController(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditLogService auditLogService)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditLogService = auditLogService;
    }

    // 🔹 1. Gabungkan GetUsers (Satu Endpoint untuk List & Paged)
    [HttpGet]
    [ProducesResponseType(typeof(BaseResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] UserFilter filter)
    {
        var query = _userManager.Users.AsQueryable();

        // Pencarian
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(u => u.UserName.Contains(filter.Search) || u.Email.Contains(filter.Search));
        }

        var totalItems = query.Count();

        // Pagination logic
        var users = query
            .OrderBy(u => u.UserName)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var pagedResult = new PagedResult<UserDto>
        {
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalItems = totalItems,
            PageCount = (int)Math.Ceiling((double)totalItems / filter.PageSize),
            Items = new List<UserDto>()
        };

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            pagedResult.Items.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                IsActive = user.EmailConfirmed,
                Roles = roles
            });
        }

        return Ok(BaseResponse<PagedResult<UserDto>>.SuccessResponse(pagedResult));
    }

    [HttpGet("{userId}/roles")]
    public async Task<ActionResult<BaseResponse<List<string>>>> GetUserRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(BaseResponse<List<string>>.FailureResponse("User not found."));

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(BaseResponse<List<string>>.SuccessResponse(roles.ToList()));
    }

    [HttpGet("by-role/{roleName}")]
    public async Task<ActionResult<BaseResponse<List<ApplicationUser>>>> GetUsersByRole(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
            return BadRequest(BaseResponse<List<ApplicationUser>>.FailureResponse("Role does not exist."));

        var users = await _userManager.GetUsersInRoleAsync(roleName);
        return Ok(BaseResponse<List<ApplicationUser>>.SuccessResponse(users.ToList()));
    }

    // 🔹 2. Role Management (Gunakan Body DTO agar lebih RESTful)
    [HttpPost("{userId}/roles/assign")]
    public async Task<ActionResult<BaseResponse<string>>> AssignRole(string userId, [FromBody] AssignRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return BadRequest(BaseResponse<string>.FailureResponse("Role does not exist."));

        var result = await _userManager.AddToRoleAsync(user, dto.Role);
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to assign role."));

        await _auditLogService.LogAsync("AssignRole", User.Identity?.Name, userId, $"Assigned role {dto.Role}");
        return Ok(BaseResponse<string>.SuccessResponse($"Role '{dto.Role}' assigned."));
    }

    [HttpPost("{userId}/roles/remove")]
    public async Task<ActionResult<BaseResponse<string>>> RemoveRole(string userId, [FromBody] AssignRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var result = await _userManager.RemoveFromRoleAsync(user, dto.Role);
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to remove role."));

        await _auditLogService.LogAsync("RemoveRole", User.Identity?.Name, userId, $"Removed role {dto.Role}");
        return Ok(BaseResponse<string>.SuccessResponse($"Role '{dto.Role}' removed."));
    }

    // 🔹 3. Account Status & Security
    [HttpPost("{userId}/lock")]
    public async Task<ActionResult<BaseResponse<string>>> LockUser(string userId, [FromQuery] int days = 365)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var until = DateTimeOffset.UtcNow.AddDays(days);
        await _userManager.SetLockoutEndDateAsync(user, until);
        return Ok(BaseResponse<string>.SuccessResponse($"User locked until {until}."));
    }

    [HttpPost("{userId}/unlock")]
    public async Task<ActionResult<BaseResponse<string>>> UnlockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        await _userManager.SetLockoutEndDateAsync(user, null);
        return Ok(BaseResponse<string>.SuccessResponse("User unlocked."));
    }

    [HttpPost("{userId}/reset-password")]
    public async Task<ActionResult<BaseResponse<string>>> ResetPassword(string userId, [FromBody] ResetPasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded) return BadRequest(BaseResponse<string>.FailureResponse("Reset failed."));

        await _auditLogService.LogAsync("ResetPassword", User.Identity?.Name, userId, "Password reset.");
        return Ok(BaseResponse<string>.SuccessResponse("Password reset successfully."));
    }

    [HttpPost("activate")]
    public async Task<ActionResult<BaseResponse<string>>> ActivateUser([FromQuery] string userId, [FromQuery] string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded) return BadRequest(BaseResponse<string>.FailureResponse("Invalid token."));

        user.IsActive = true; // Jika Anda punya properti IsActive custom
        await _userManager.UpdateAsync(user);

        await _auditLogService.LogAsync("Activate", User.Identity?.Name, userId, "User activated.");
        return Ok(BaseResponse<string>.SuccessResponse("User activated."));
    }

    [HttpPost("{userId}/deactivate")]
    public async Task<ActionResult<BaseResponse<string>>> DeactivateUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        user.EmailConfirmed = false;
        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        await _auditLogService.LogAsync("Deactivate", User.Identity?.Name, userId, "User deactivated.");
        return Ok(BaseResponse<string>.SuccessResponse("User deactivated."));
    }
}