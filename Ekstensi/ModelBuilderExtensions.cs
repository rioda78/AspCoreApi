using AspCoreApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Reflection.Emit;

namespace AspCoreApi.Ekstensi
{
    public static class ModelBuilderExtensions
    {
        public static void RegisterAllEntities<TBaseType>(this ModelBuilder modelBuilder, Assembly assembly)
        {
            var entityTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(TBaseType).IsAssignableFrom(t));

            foreach (var type in entityTypes)
            {
                modelBuilder.Entity(type);
            }
        }

        public static void SeedIdentitas(this ModelBuilder builder)
        {

            //a hasher to hash the password before seeding the user to the db
            //   var hasher = new PasswordHasher<ApplicationUser>();
            //password = Admin@123

            //Seeding the User to AspNetUsers table
            builder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = "6042BC0C-61A6-4438-B77D-9FD26E77016C", // primary key
                    Email = "sayonara@gmail.com",
                    FullName = "Sayonara",
                    UserName = "myAqua",
                    NormalizedUserName = "MYAQUA",
                    PasswordHash = "AQAAAAIAAYagAAAAEHboxtSxYdkmMBkRKKzmxeJMygxgKzPcPamvS/Uxfk8ZekaPelWha0R4RxZ5rwJaKg==",// hasher.HashPassword(null, "Password123"),
                    ConcurrencyStamp = "6042BC0C-61A6-4438-B77D-9FD26E77016C",
                    IsActive = true,
                    SecurityStamp = "6042BC0C-61A6-4438-B77D-9FD26E77016C",
                    LastLoginDate = new DateTime(2026,1,1),
                    Gender = GenderType.Male,
                },
                new ApplicationUser
                {
                    Id = "E2D47B58-A22D-4165-B100-2DE46D067498", // primary key
                    Email = "AdminGendeng@gmail.com",
                    FullName = "Admin Genteng",
                    UserName = "UdemAdmin",
                    NormalizedUserName = "UDEMADMIN",
                    ConcurrencyStamp = "E2D47B58-A22D-4165-B100-2DE46D067498",
                    PasswordHash = "AQAAAAIAAYagAAAAEHboxtSxYdkmMBkRKKzmxeJMygxgKzPcPamvS/Uxfk8ZekaPelWha0R4RxZ5rwJaKg==", //hasher.HashPassword(null, "Password123??"),
                    IsActive = true,
                    SecurityStamp = "E2D47B58-A22D-4165-B100-2DE46D067498",
                    LastLoginDate = new DateTime(2026, 1, 1),
                    Gender = GenderType.Male,
                }
            );

            List<IdentityRole> roles = new List<IdentityRole>()
                    {
                        new IdentityRole { Id ="A27149DC-7E61-4055-B516-A96189210CEA", ConcurrencyStamp ="A27149DC-7E61-4055-B516-A96189210CEA",
                            Name = "Admin", NormalizedName = "ADMIN"
                        },
                        new IdentityRole { Id ="06069DC4-7C5A-4B13-A08C-85C8C128932A", ConcurrencyStamp="06069DC4-7C5A-4B13-A08C-85C8C128932A",
                            Name = "User", NormalizedName = "USER" }
                    };

            builder.Entity<IdentityRole>().HasData(roles);

            //Seeding the relation between our user and role to AspNetUserRoles table
            builder.Entity<IdentityUserRole<string>>().HasData(
                //admin
                new IdentityUserRole<string>
                {
                    RoleId = "A27149DC-7E61-4055-B516-A96189210CEA",
                    UserId = "E2D47B58-A22D-4165-B100-2DE46D067498"
                }, 
                new IdentityUserRole<string>
                {
                    RoleId = "06069DC4-7C5A-4B13-A08C-85C8C128932A",
                    UserId = "6042BC0C-61A6-4438-B77D-9FD26E77016C"
                }
            );
        }
        public static void ChangeTableNameAsp(this ModelBuilder builder)
        {
            const int maxKeyLength = 128;
            builder.Entity<ApplicationUser>(b =>
            {
                // Primary key
                b.HasKey(u => u.Id);

                // Indexes for "normalized" username and email, to allow efficient lookups
                b.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
                b.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");

                // Maps to the AspNetUsers table
                b.ToTable("Users");

                // A concurrency token for use with the optimistic concurrency checking
                b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

                // Limit the size of columns to use efficient database types
                b.Property(u => u.UserName).HasMaxLength(256);
                b.Property(u => u.NormalizedUserName).HasMaxLength(256);
                b.Property(u => u.Email).HasMaxLength(256);
                b.Property(u => u.NormalizedEmail).HasMaxLength(256);

                // The relationships between User and other entity types
                // Note that these relationships are configured with no navigation properties

                // Each User can have many UserClaims

                b.HasMany<IdentityUserClaim<string>>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
                // Each User can have many UserLogins
                b.HasMany<IdentityUserLogin<string>>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();

                // Each User can have many UserTokens
                b.HasMany<IdentityUserToken<string>>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();

                // Each User can have many entries in the UserRole join table
                b.HasMany<IdentityUserRole<string>>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
            });

            builder.Entity<IdentityUserClaim<string>>(b =>
            {
                // Primary key
                b.HasKey(uc => uc.Id);

                // Maps to the AspNetUserClaims table
                b.ToTable("UserClaims");
            });

            builder.Entity<IdentityUserLogin<string>>(b =>
            {
                // Composite primary key consisting of the LoginProvider and the key to use
                // with that provider
                b.HasKey(l => new { l.LoginProvider, l.ProviderKey });

                // Limit the size of the composite key columns due to common DB restrictions
                b.Property(l => l.LoginProvider).HasMaxLength(128);
                b.Property(l => l.ProviderKey).HasMaxLength(128);

                // Maps to the AspNetUserLogins table
                b.ToTable("UserLogins");
            });

            builder.Entity<IdentityUserToken<string>>(b =>
            {
                // Composite primary key consisting of the UserId, LoginProvider and Name
                b.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

                // Limit the size of the composite key columns due to common DB restrictions
                b.Property(t => t.LoginProvider).HasMaxLength(maxKeyLength);
                b.Property(t => t.Name).HasMaxLength(maxKeyLength);

                // Maps to the AspNetUserTokens table
                b.ToTable("UserTokens");
            });

            builder.Entity<IdentityRole>(b =>
            {
                // Primary key
                b.HasKey(r => r.Id);

                // Index for "normalized" role name to allow efficient lookups
                b.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();

                // Maps to the AspNetRoles table
                b.ToTable("Roles");

                // A concurrency token for use with the optimistic concurrency checking
                b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

                // Limit the size of columns to use efficient database types
                b.Property(u => u.Name).HasMaxLength(256);
                b.Property(u => u.NormalizedName).HasMaxLength(256);

                // The relationships between Role and other entity types
                // Note that these relationships are configured with no navigation properties

                // Each Role can have many entries in the UserRole join table
                b.HasMany<IdentityUserRole<string>>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();

                // Each Role can have many associated RoleClaims
                b.HasMany<IdentityRoleClaim<string>>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
            });

            builder.Entity<IdentityRoleClaim<string>>(b =>
            {
                // Primary key
                b.HasKey(rc => rc.Id);

                // Maps to the AspNetRoleClaims table
                b.ToTable("RoleClaims");
            });

            builder.Entity<IdentityUserRole<string>>(b =>
            {
                // Primary key
                b.HasKey(r => new { r.UserId, r.RoleId });

                // Maps to the AspNetUserRoles table
                b.ToTable("UserRoles");
            });
        }
        public static void RegisterEntityTypeConfiguration(this ModelBuilder modelBuilder, Assembly assembly)
        {
            var applyGenericMethod = typeof(ModelBuilder)
                .GetMethods()
                .First(m => m.Name == nameof(ModelBuilder.ApplyConfiguration) && m.GetParameters().Length == 1);

            var typesToRegister = assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Select(t => new
                {
                    Type = t,
                    Interface = t.GetInterfaces()
                        .FirstOrDefault(i =>
                            i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                })
                .Where(t => t.Interface != null);

            foreach (var config in typesToRegister)
            {
                var entityType = config.Interface!.GenericTypeArguments[0];
                var applyConcreteMethod = applyGenericMethod.MakeGenericMethod(entityType);
                var configurationInstance = Activator.CreateInstance(config.Type)!;
                applyConcreteMethod.Invoke(modelBuilder, new[] { configurationInstance });
            }
        }

    }

}
