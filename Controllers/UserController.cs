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
    
    
    // 🔹 Assign role to user
    [HttpPost("{userId}/roles/assign")]
    public async Task<ActionResult<BaseResponse<string>>> AssignRole(string userId, [FromQuery] string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        if (!await _roleManager.RoleExistsAsync(role))
            return BadRequest(BaseResponse<string>.FailureResponse("Role does not exist."));

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse(string.Join(", ", result.Errors.Select(e => e.Description))));

        return Ok(BaseResponse<string>.SuccessResponse($"Role '{role}' assigned to user."));
    }

    // 🔹 Remove role from user
    [HttpPost("{userId}/roles/remove")]
    public async Task<ActionResult<BaseResponse<string>>> RemoveRole(string userId, [FromQuery] string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        if (!await _userManager.IsInRoleAsync(user, role))
            return BadRequest(BaseResponse<string>.FailureResponse("User is not in the specified role."));

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse(string.Join(", ", result.Errors.Select(e => e.Description))));

        return Ok(BaseResponse<string>.SuccessResponse($"Role '{role}' removed from user."));
    }

    // 🔹 Get user roles
    [HttpGet("{userId}/roles")]
    public async Task<ActionResult<BaseResponse<List<string>>>> GetUserRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(BaseResponse<List<string>>.FailureResponse("User not found."));

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(BaseResponse<List<string>>.SuccessResponse(roles.ToList()));
    }
    

    // 🔹 Lock user
    [HttpPost("{userId}/lock")]
    public async Task<ActionResult<BaseResponse<string>>> LockUser(string userId, [FromQuery] int days = 365)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var until = DateTimeOffset.UtcNow.AddDays(days);
        var result = await _userManager.SetLockoutEndDateAsync(user, until);

        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to lock user."));

        return Ok(BaseResponse<string>.SuccessResponse($"User locked until {until}."));
    }

    // 🔹 Unlock user
    [HttpPost("{userId}/unlock")]
    public async Task<ActionResult<BaseResponse<string>>> UnlockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var result = await _userManager.SetLockoutEndDateAsync(user, null);

        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to unlock user."));

        return Ok(BaseResponse<string>.SuccessResponse("User unlocked."));
    }
    // 🔹 Reactivate user (IsActive = true)
    [HttpPost("{userId}/reactivate")]
    public async Task<ActionResult<BaseResponse<string>>> ReactivateUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        return Ok(BaseResponse<string>.SuccessResponse("User reactivated."));
    }

    // 🔹 Get users by role
    [HttpGet("by-role/{roleName}")]
    public async Task<ActionResult<BaseResponse<List<ApplicationUser>>>> GetUsersByRole(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
            return BadRequest(BaseResponse<List<ApplicationUser>>.FailureResponse("Role does not exist."));

        var users = await _userManager.GetUsersInRoleAsync(roleName);
        return Ok(BaseResponse<List<ApplicationUser>>.SuccessResponse(users.ToList()));
    }
    
    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<UserDto>>>> GetUsers()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                IsActive = user.EmailConfirmed,
                Roles = roles
            });
        }

        return Ok(BaseResponse<List<UserDto>>.SuccessResponse(result));
    }
    
     [HttpPost("activate")]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<string>>> ActivateUser([FromQuery] string userId, [FromQuery] string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Invalid token or already confirmed."));

        await _auditLogService.LogAsync("Activate", User.Identity?.Name, userId, "User activated via email confirmation");
        return Ok(BaseResponse<string>.SuccessResponse("User activated successfully."));
    }

    [HttpGet]
    [ProducesResponseType(typeof(BaseResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] UserFilter filter)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(u => u.UserName.Contains(filter.Search) || u.Email.Contains(filter.Search));
        }

        var totalItems = query.Count();
        var users = query
            .OrderBy(u => u.UserName)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var result = new PagedResult<UserDto>
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
            result.Items.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                IsActive = user.EmailConfirmed,
                Roles = roles
            });
        }

        return Ok(BaseResponse<PagedResult<UserDto>>.SuccessResponse(result));
    }

    [HttpPost("{userId}/assign-role")]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<string>>> AssignRoleToUser(string userId, [FromBody] AssignRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
        if (!roleExists) return BadRequest(BaseResponse<string>.FailureResponse("Role does not exist."));

        var result = await _userManager.AddToRoleAsync(user, dto.Role);
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to assign role."));

        await _auditLogService.LogAsync("AssignRole", User.Identity?.Name, userId, $"Assigned role {dto.Role}");
        return Ok(BaseResponse<string>.SuccessResponse("Role assigned successfully."));
    }

    [HttpPost("{userId}/remove-role")]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<string>>> RemoveRoleFromUser(string userId, [FromBody] AssignRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var result = await _userManager.RemoveFromRoleAsync(user, dto.Role);
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to remove role."));

        await _auditLogService.LogAsync("RemoveRole", User.Identity?.Name, userId, $"Removed role {dto.Role}");
        return Ok(BaseResponse<string>.SuccessResponse("Role removed successfully."));
    }

    [HttpPost("{userId}/reset-password")]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<string>>> ResetPassword(string userId, [FromBody] ResetPasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to reset password."));

        await _auditLogService.LogAsync("ResetPassword", User.Identity?.Name, userId, "User password reset.");
        return Ok(BaseResponse<string>.SuccessResponse("Password reset successfully."));
    }

    [HttpPost("{userId}/deactivate")]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<string>>> DeactivateUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound(BaseResponse<string>.FailureResponse("User not found."));

        user.EmailConfirmed = false;
        await _userManager.UpdateAsync(user);

        await _auditLogService.LogAsync("Deactivate", User.Identity?.Name, userId, "User deactivated.");
        return Ok(BaseResponse<string>.SuccessResponse("User deactivated."));
    }


}