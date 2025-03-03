using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using Ardalis.GuardClauses;
using BuildingBlocks.Core.Exception.Types;
using BuildingBlocks.Core.Extensions;
using BuildingBlocks.Core.Web.Extenions;
using BuildingBlocks.Security.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Security.Extensions;

public static class Extensions
{
    public static AuthenticationBuilder AddCustomJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtOptions>? optionConfigurator = null)
    {
        // https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/415
        // https://mderriey.com/2019/06/23/where-are-my-jwt-claims/
        // https://leastprivilege.com/2017/11/15/missing-claims-in-the-asp-net-core-2-openid-connect-handler/
        // https://stackoverflow.com/a/50012477/581476
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

        AddJwtServices(services, configuration, optionConfigurator);

        var jwtOptions = configuration.BindOptions<JwtOptions>(nameof(JwtOptions));
        Guard.Against.Null(jwtOptions, nameof(jwtOptions));

        // https://docs.microsoft.com/en-us/aspnet/core/security/authentication
        // https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme?view=aspnetcore-6.0#use-multiple-authentication-schemes
        // https://auth0.com/blog/whats-new-in-dotnet-7-for-authentication-and-authorization/
        // since .NET 7, the default scheme is no longer required, when we define just one authentication scheme and It is automatically inferred
        return services.AddAuthentication() // no default scheme specified
            .AddJwtBearer(options =>
            {
                //-- JwtBearerDefaults.AuthenticationScheme --
                options.Audience = jwtOptions.Audience;
                options.SaveToken = true;
                options.RefreshOnIssuerKeyNotFound = false;
                options.RequireHttpsMetadata = false;
                options.IncludeErrorDetails = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    SaveSigninToken = true,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            throw new UnAuthorizedException("The Token is expired.");
                        }

                        throw new IdentityException(
                            context.Exception.Message,
                            statusCode: HttpStatusCode.InternalServerError);
                    },
                    OnChallenge = context =>
                    {
                        // context.HandleResponse();
                        // if (!context.Response.HasStarted)
                        // {
                        //     throw new IdentityException(
                        //         "You are not Authorized.",
                        //         statusCode: HttpStatusCode.Unauthorized);
                        // }

                        return Task.CompletedTask;
                    },
                    OnForbidden = _ =>
                        throw new ForbiddenException("You are not authorized to access this resource.")
                };
            });
    }

    public static IServiceCollection AddJwtServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtOptions>? optionConfigurator = null)
    {
        var jwtOptions = configuration.BindOptions<JwtOptions>(nameof(JwtOptions));
        Guard.Against.Null(jwtOptions, nameof(jwtOptions));

        optionConfigurator?.Invoke(jwtOptions);

        if (optionConfigurator is { })
        {
            services.Configure(nameof(JwtOptions), optionConfigurator);
        }
        else
        {
            services.AddOptions<JwtOptions>().Bind(configuration.GetSection(nameof(JwtOptions)))
                .ValidateDataAnnotations();
        }

        services.AddTransient<IJwtService, JwtService>();

        return services;
    }

    public static IServiceCollection AddCustomAuthorization(
        this IServiceCollection services,
        IList<ClaimPolicy>? claimPolicies = null,
        IList<RolePolicy>? rolePolicies = null)
    {
        services.AddAuthorization(authorizationOptions =>
        {
            // https://docs.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme
            // https://andrewlock.net/setting-global-authorization-policies-using-the-defaultpolicy-and-the-fallbackpolicy-in-aspnet-core-3/
            var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme);
            defaultAuthorizationPolicyBuilder =
                defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
            authorizationOptions.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();

            // https://docs.microsoft.com/en-us/aspnet/core/security/authorization/claims
            if (claimPolicies is { })
            {
                foreach (var policy in claimPolicies)
                {
                    authorizationOptions.AddPolicy(policy.Name, x =>
                    {
                        x.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                        foreach (var policyClaim in policy.Claims)
                        {
                            x.RequireClaim(policyClaim.Type, policyClaim.Value);
                        }
                    });
                }
            }

            // https://docs.microsoft.com/en-us/aspnet/core/security/authorization
            if (rolePolicies is { })
            {
                foreach (var rolePolicy in rolePolicies)
                {
                    authorizationOptions.AddPolicy(rolePolicy.Name, x =>
                    {
                        x.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                        x.RequireRole(rolePolicy.Roles);
                    });
                }
            }
        });

        return services;
    }

    public static void AddExternalLogins(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.BindOptions<JwtOptions>(nameof(JwtOptions));
        Guard.Against.Null(jwtOptions, nameof(jwtOptions));

        if (jwtOptions.GoogleLoginConfigs is { })
        {
            services.AddAuthentication()
                .AddGoogle(googleOptions =>
                {
                    googleOptions.ClientId = jwtOptions.GoogleLoginConfigs.ClientId;
                    googleOptions.ClientSecret = jwtOptions.GoogleLoginConfigs.ClientId;
                    googleOptions.SaveTokens = true;
                });
        }
    }
}
