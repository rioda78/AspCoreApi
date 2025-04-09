namespace AspCoreApi.ViewModel;

public class UserDto
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    public IList<string> Roles { get; set; }
}

public class AssignRoleDto
{
    public string Role { get; set; }
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; }
}