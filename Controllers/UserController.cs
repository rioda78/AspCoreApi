using AspCoreApi.Data;
using AspCoreApi.Filters;
using AspCoreApi.Helpers;
using AspCoreApi.Models;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ApplicationUser>>> GetUsers([FromQuery] UserFilter filter)
    {
        var query = _context.Users.AsQueryable();

        // Filtering
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(u => u.UserName.Contains(filter.Search));
        }

        // Paging
        var paged = query
            .OrderBy(u => u.UserName)
            .ToPagedList(filter.PageNumber, filter.PageSize);


        // Wrap result
        var result = new PagedResult<ApplicationUser>
        {
            Items = paged.ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItemCount,
            PageCount = paged.PageCount
        };

        return Ok(result);
    }

}
