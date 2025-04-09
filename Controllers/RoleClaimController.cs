using System.Security.Claims;
using AspCoreApi.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AspCoreApi.Controllers;


[ApiController]
[Route("api/[controller]")]
public class RoleClaimsController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleClaimsController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }
    
    [HttpPost("{role}/claims")]
    public async Task<ActionResult<BaseResponse<string>>> AddClaimToRole(string role, [FromQuery] string claimType, [FromQuery] string claimValue)
    {
        var identityRole = await _roleManager.FindByNameAsync(role);
        if (identityRole == null)
            return NotFound(BaseResponse<string>.FailureResponse("Role not found."));

        var result = await _roleManager.AddClaimAsync(identityRole, new Claim(claimType, claimValue));
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to add claim."));

        return Ok(BaseResponse<string>.SuccessResponse("Claim added to role."));
    }

    [HttpDelete("{role}/claims")]
    public async Task<ActionResult<BaseResponse<string>>> RemoveClaimFromRole(string role, [FromQuery] string claimType, [FromQuery] string claimValue)
    {
        var identityRole = await _roleManager.FindByNameAsync(role);
        if (identityRole == null)
            return NotFound(BaseResponse<string>.FailureResponse("Role not found."));

        var result = await _roleManager.RemoveClaimAsync(identityRole, new Claim(claimType, claimValue));
        if (!result.Succeeded)
            return BadRequest(BaseResponse<string>.FailureResponse("Failed to remove claim."));

        return Ok(BaseResponse<string>.SuccessResponse("Claim removed from role."));
    }

    [HttpGet("{role}/claims")]
    public async Task<ActionResult<BaseResponse<List<Claim>>>> GetClaimsOfRole(string role)
    {
        var identityRole = await _roleManager.FindByNameAsync(role);
        if (identityRole == null)
            return NotFound(BaseResponse<List<Claim>>.FailureResponse("Role not found."));

        var claims = await _roleManager.GetClaimsAsync(identityRole);
        return Ok(BaseResponse<List<Claim>>.SuccessResponse(claims.ToList()));
    }
}