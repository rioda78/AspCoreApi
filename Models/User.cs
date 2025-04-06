using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspCoreApi.Models;

public class User : IdentityUser<int>, IEntity
{
    public User()
    {
        IsActive = true;
    }
        
    [Required]
    [StringLength(100)]
    public string FullName { get; set; }
    public int Age { get; set; }
    public GenderType Gender { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }

   // public ICollection<Post> Posts { get; set; }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(p => p.UserName).IsRequired().HasMaxLength(100);
    }
}

public enum GenderType
{
    Male = 1,
    Female = 2
}