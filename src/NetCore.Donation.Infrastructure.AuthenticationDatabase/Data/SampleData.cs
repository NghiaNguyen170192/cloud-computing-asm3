using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Infrastructure.AuthenticationDatabase.Models;

namespace NetCore.Donation.Infrastructure.AuthenticationDatabase.Data
{
    public static class SampleData
    {
        //private static async Task AddUserRole(IApplicationBuilder app, string userName, string roleName)
        //{
        //    using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
        //    {
        //        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        //        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        //        string defaultPassword = "Qwert@123";
        //        ApplicationUser user = new ApplicationUser
        //        {
        //            UserName = userName,
        //            NormalizedUserName = userName.ToLower(),
        //            Email = userName,
        //            NormalizedEmail = userName,
        //            EmailConfirmed = true,
        //            LockoutEnabled = false,
        //            SecurityStamp = Guid.NewGuid().ToString()
        //        };

        //        IdentityRole identityRole = new IdentityRole { Name = roleName, NormalizedName = roleName };
        //        if (roleManager.FindByNameAsync(roleName).Result == null)
        //        {
        //            await roleManager.CreateAsync(identityRole);
        //        }

        //        if (userManager.FindByNameAsync(userName).Result == null)
        //        {
        //            IdentityResult result = userManager.CreateAsync(user, defaultPassword).Result;

        //            if (result.Succeeded)
        //            {
        //                userManager.AddToRoleAsync(user, identityRole.Name).Wait();
        //            }
        //        }
        //    }
        //}

        //public static async void SeedUsersAndRoles(IApplicationBuilder app)
        //{
        //    await AddUserRole(app, "admin@netcore.com", "admin");
        //    await AddUserRole(app, "manager@netcore.com", "manager");
        //    await AddUserRole(app, "user@netcore.com", "user");
        //}

        public static void Migration(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            using var persistedGrantDbContext = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
            using var configurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            using var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (persistedGrantDbContext.Database.GetPendingMigrations().Any())
            {
                persistedGrantDbContext.Database.Migrate();
            }

            if (configurationDbContext.Database.GetPendingMigrations().Any())
            {
                configurationDbContext.Database.Migrate();
            }

            if (applicationDbContext.Database.GetPendingMigrations().Any())
            {
                applicationDbContext.Database.Migrate();
            }
        }

        public class Users
        {
            public static List<TestUser> Get()
            {
                return new List<TestUser> {
                    new TestUser {
                        SubjectId = Guid.NewGuid().ToString(),
                        Username = "manager",
                        Password ="your_password_here",
                        Claims = new List<Claim> {
                            new Claim(JwtClaimTypes.Role, "manager"),
                        }
                    },

                    new TestUser {
                        SubjectId = Guid.NewGuid().ToString(),
                        Username = "admin",
                        Password = "your_password_here",
                        Claims = new List<Claim> {
                            new Claim(JwtClaimTypes.Role, "admin"),
                        }
                    },

                    new TestUser {
                        SubjectId = Guid.NewGuid().ToString(),
                        Username = "user",
                        Password = "your_password_here",
                        Claims = new List<Claim> {
                            new Claim(JwtClaimTypes.Role, "user"),
                        }
                    }
                };
            }
        }

        public static async void InitializeDbData(IApplicationBuilder app, string secret)
        {
            using var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

            if (!context.Clients.Any())
            {
                foreach (var client in Config.Config.GetClients())
                {
                    client.ClientSecrets = GetSecrets(secret);
                    context.Clients.Add(client.ToEntity());
                }
                await context.SaveChangesAsync();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in Config.Config.GetIdentityResources())
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                await context.SaveChangesAsync();
            }

            if (!context.ApiScopes.Any())
            {
                foreach (var resource in Config.Config.GetApiScopes())
                {
                    context.ApiScopes.Add(resource.ToEntity());
                }
                await context.SaveChangesAsync();
            }

            if (!context.ApiResources.Any())
            {
                foreach (var resource in Config.Config.GetApiResources())
                {
                    context.ApiResources.Add(resource.ToEntity());
                }
                await context.SaveChangesAsync();
            }

            if (userManager.Users.Any() && roleManager.Roles.Any())
            {
                return;
            }

            foreach (var testUser in Users.Get())
            {
                var identityUser = new ApplicationUser(testUser.Username)
                {
                    Id = testUser.SubjectId,
                };

                var result = await userManager.CreateAsync(identityUser, testUser.Password);
                if (result.Succeeded)
                {
                    await userManager.AddClaimsAsync(identityUser, testUser.Claims.ToList());
                    var claimRole = testUser.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Role);
                    if (claimRole != null)
                    {
                        var identityRole = new IdentityRole(claimRole.Value);
                        await roleManager.CreateAsync(identityRole);
                        await userManager.AddToRoleAsync(identityUser, identityRole.Name);
                    }
                }
            }
        }

        private static List<Secret> GetSecrets(string secret)
        {
            List<Secret> secrets = new()
            {
                new Secret(secret.Sha256())
            };

            return secrets;
        }
    }
}
