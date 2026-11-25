using System.Text;
using Berynx.Api.Auth;
using Berynx.Api.Onboarding;
using Berynx.Api.IdentityAccess;
using Berynx.Application.IdentityAccess;
using Berynx.Infrastructure.Persistence;
using Berynx.Api.Errors;
using Berynx.Api.Masters;
using Berynx.Application.Masters;
using Berynx.Application.Tenancy;
using Berynx.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantContext, HttpCurrentTenantContext>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityCore<M02IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<BerynxDbContext>()
    .AddDefaultTokenProviders();
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Berynx.Construction360");
var dataProtectionDirectory = builder.Configuration["DataProtection:KeyDirectory"];
if (!string.IsNullOrWhiteSpace(dataProtectionDirectory))
{
    Directory.CreateDirectory(dataProtectionDirectory);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDirectory));
}
else if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "Configure DataProtection:KeyDirectory as a durable, shared, access-controlled key-ring location.");
}
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(1));
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("m02-auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddScoped<M02AuthenticationService>();
builder.Services.AddScoped<M03MasterService>();
builder.Services.AddScoped<IM03CreditFactsProvider, M03UnavailableCreditFactsProvider>();
builder.Services.AddScoped<IM03CreditProfileProvider, M03CreditProfileProvider>();
builder.Services.AddScoped<M03CreditEvaluationService>();
builder.Services.AddScoped<OnboardingStore>();
builder.Services.AddScoped<TenantEntitlementService>();
builder.Services.AddScoped<TenantAuthorizationService>();
builder.Services.AddScoped<IBranchAccess, ClaimBranchAccess>();
builder.Services.AddScoped<IM02Access>(services =>
    new M02AccessService(services.GetRequiredService<TenantAuthorizationService>()));
builder.Services.AddScoped<IM02InvitationDeliveryProvider, ConfiguredM02InvitationDeliveryProvider>();
builder.Services.AddScoped<IM02ApprovalEngine, M02ApprovalEngine>();
builder.Services.AddScoped<IM02TaskReassignmentProvider, M02ApprovalTaskReassignmentProvider>();
builder.Services.AddScoped<M02AccountEmailSender>();
builder.Services.AddScoped<M02InviteTokenService>();
builder.Services.AddHostedService<M02ApprovalEscalationService>();
builder.Services.AddHostedService<M02InvitationDeliveryService>();
builder.Services.AddScoped<Berynx.Application.Onboarding.IOpeningItems, OpeningItems>();
builder.Services.AddScoped<Berynx.Application.Onboarding.IPostingNumbers, PostingNumbers>();
builder.Services.AddHttpClient("invitation", client => client.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient("identity-provider", client => client.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHostedService<InvitationDelivery>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        var signingKey = builder.Configuration["Jwt:SigningKey"];
        if (!string.IsNullOrWhiteSpace(signingKey) &&
            Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            IssuerSigningKey = string.IsNullOrWhiteSpace(signingKey)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (!Guid.TryParse(principal?.FindFirst("sub")?.Value, out var identityUserId) ||
                    !Guid.TryParse(principal?.FindFirst("tenant_id")?.Value, out var tenantId) ||
                    !Guid.TryParse(principal?.FindFirst("sid")?.Value, out var sessionId))
                {
                    context.Fail("Invalid session claims.");
                    return;
                }

                var authentication = context.HttpContext.RequestServices
                    .GetRequiredService<M02AuthenticationService>();
                if (!await authentication.ValidateSession(tenantId, identityUserId, sessionId,
                        context.HttpContext.RequestAborted))
                    context.Fail("Session is no longer active.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddPolicy(
            "LocalAngular",
            policy => policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));
}

var app = builder.Build();

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalAngular");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapOnboarding();
app.MapPlatformSaaS();
app.MapM02();
app.MapM03();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet(
        "/api/v1/status",
        () => Results.Ok(new { service = "Berynx Construction 360 API", version = "v1" }))
    .WithName("GetApiStatus");

app.Run();

public partial class Program { }
