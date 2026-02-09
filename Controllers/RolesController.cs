using AspCoreApi.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AspCoreApi.Controllers;


[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolesController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    // 1. Gabungkan GetAllRoles dan GetRoles menjadi satu
    [HttpGet]
    [ProducesResponseType(typeof(BaseResponse<List<IdentityRole>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<List<IdentityRole>>>> GetAllRoles()
    {
        var roles = _roleManager.Roles.ToList();
        return Ok(BaseResponse<List<IdentityRole>>.SuccessResponse(roles));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BaseResponse<IdentityRole>>> GetRoleById(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(BaseResponse<IdentityRole>.FailureResponse("Role not found."));

        return Ok(BaseResponse<IdentityRole>.SuccessResponse(role));
    }

    // 2. Gunakan rute yang jelas untuk pembaruan berdasarkan ID
    [HttpPut("{id}")]
    public async Task<ActionResult<BaseResponse<string>>> UpdateRole(string id, [FromBody] string newName)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound(BaseResponse<string>.FailureResponse("Role not found."));

        role.Name = newName;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(BaseResponse<string>.FailureResponse(errorMsg));
        }

        return Ok(BaseResponse<string>.SuccessResponse("Role updated successfully."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<string>>> CreateRole([FromQuery] string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
            return BadRequest(BaseResponse<string>.FailureResponse("Role already exists."));

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to create role."));

        return Ok(BaseResponse<string>.SuccessResponse("Role created successfully."));
    }

    [HttpDelete]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<string>>> DeleteRole([FromQuery] string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
            return NotFound(BaseResponse<string>.FailureResponse("Role not found."));

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to delete role."));

        return Ok(BaseResponse<string>.SuccessResponse("Role deleted successfully."));
    }

    // 3. Tambahkan sub-route "rename" agar tidak bentrok dengan Put berdasarkan ID
    [HttpPut("rename")]
    [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<string>>> RenameRole([FromQuery] string currentName, [FromQuery] string newName)
    {
        var role = await _roleManager.FindByNameAsync(currentName);
        if (role == null)
            return NotFound(BaseResponse<string>.FailureResponse("Role not found."));

        role.Name = newName;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to rename role."));

        return Ok(BaseResponse<string>.SuccessResponse("Role renamed successfully."));
    }
}
