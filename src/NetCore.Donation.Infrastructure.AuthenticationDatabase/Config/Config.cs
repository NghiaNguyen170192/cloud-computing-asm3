using System.Collections.Generic;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;

namespace NetCore.Donation.Infrastructure.AuthenticationDatabase.Config
{
    public class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Email(),
                new IdentityResources.Profile(),
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource("netcore-api")
                {
                    UserClaims =
                    {
                        JwtClaimTypes.Email,
                        JwtClaimTypes.PhoneNumber,
                        JwtClaimTypes.GivenName,
                        JwtClaimTypes.FamilyName,
                        JwtClaimTypes.Role,
                    },
                    Scopes =
                    {
                         "netcore-api",
                         IdentityServerConstants.StandardScopes.OfflineAccess
                    }
                }
            };
        }


        public static IEnumerable<ApiScope> GetApiScopes()
        {
            return new ApiScope[]
            {
                new ApiScope("netcore-api"),
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId = "netcore.client",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    AllowOfflineAccess = true,
                    AllowedScopes =
                    {
                        "netcore-api",
                        IdentityServerConstants.StandardScopes.OfflineAccess
                    },
                    RefreshTokenExpiration = TokenExpiration.Absolute,
                    RefreshTokenUsage = TokenUsage.ReUse,
                    AbsoluteRefreshTokenLifetime = 200,
                    AccessTokenLifetime = (int) new System.TimeSpan(1, 0, 0, 0).TotalSeconds,
                    AccessTokenType = AccessTokenType.Jwt
                },
                // interactive ASP.NET Core MVC client
                new Client
                {
                    ClientId = "mvc",

                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    
                    // where to redirect to after login
                    RedirectUris = { "https://localhost:6003/signin-oidc" },

                    // where to redirect to after logout
                    PostLogoutRedirectUris = { "https://localhost:6003/signout-callback-oidc" },

                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "netcore-api"
                    }
                }
            };
        }
    }
}