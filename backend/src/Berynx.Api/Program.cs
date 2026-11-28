using System.Text;
using Berynx.Api.Auth;
using Berynx.Api.Onboarding;
using Berynx.Api.IdentityAccess;
using Berynx.Application.Accounting;
using Berynx.Application.Delivery;
using Berynx.Application.Documents;
using Berynx.Application.IdentityAccess;
using Berynx.Infrastructure.Persistence;
using Berynx.Infrastructure.Documents;
using Berynx.Api.Errors;
using Berynx.Api.Masters;
using Berynx.Api.Crm;
using Berynx.Api.Quotations;
using Berynx.Api.SalesOrders;
using Berynx.Api.Procurement;
using Berynx.Api.Delivery;
using Berynx.Api.DeliveryChallans;
using Berynx.Api.Invoicing;
using Berynx.Api.Receivables;
using Berynx.Api.Promotion;
using Berynx.Api.Quality;
using Berynx.Api.Accounting;
using Berynx.Api.Gst;
using Berynx.Api.Expenses;
using Berynx.Api.Dashboard;
using Berynx.Api.Reports;
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
if (string.IsNullOrWhiteSpace(dataProtectionDirectory) && builder.Environment.IsDevelopment())
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (!string.IsNullOrWhiteSpace(localAppData))
        dataProtectionDirectory = Path.Combine(localAppData, "BerynxConstruction360", "keys");
}
if (!string.IsNullOrWhiteSpace(dataProtectionDirectory))
{
    Directory.CreateDirectory(dataProtectionDirectory);
    if (!OperatingSystem.IsWindows())
        File.SetUnixFileMode(dataProtectionDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionDirectory));
}
else
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
    options.AddPolicy("m17-public", context =>
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
builder.Services.AddScoped<IM12PostingEngine, M12PostingEngine>();
builder.Services.Configure<M18DocumentOptions>(builder.Configuration.GetSection("M18:Documents"));
builder.Services.Configure<M18StorageOptions>(builder.Configuration.GetSection("M18:Storage"));
builder.Services.Configure<ClamAvOptions>(builder.Configuration.GetSection("M18:Scanner"));
builder.Services.AddSingleton<IM18DocumentStorage>(services =>
    new FileSystemM18DocumentStorage(services.GetRequiredService<Microsoft.Extensions.Options.IOptions<M18StorageOptions>>(), builder.Environment.WebRootPath));
builder.Services.AddSingleton<IM18MalwareScanner, ClamAvMalwareScanner>();
builder.Services.AddScoped<IM18DocumentService, M18DocumentService>();
builder.Services.AddScoped<IM08SecureEvidenceProvider, M18M08SecureEvidenceProvider>();
builder.Services.AddScoped<IM05CustomerOtpProvider, UnconfiguredM05CustomerOtpProvider>();
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
if (!builder.Environment.IsEnvironment("Testing")) {
    builder.Services.AddHostedService<M02ApprovalEscalationService>();
    builder.Services.AddHostedService<M02InvitationDeliveryService>();
}
builder.Services.AddScoped<Berynx.Application.Onboarding.IOpeningItems, OpeningItems>();
builder.Services.AddScoped<Berynx.Application.Onboarding.IPostingNumbers, PostingNumbers>();
builder.Services.AddHttpClient("invitation", client => client.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient("identity-provider", client => client.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
if (!builder.Environment.IsEnvironment("Testing")) builder.Services.AddHostedService<InvitationDelivery>();

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
app.MapM04();
app.MapM05();
app.MapM06();
app.MapM07();
app.MapM08();
app.MapM09();
app.MapM10();
app.MapM11();
app.MapM12();
app.MapM13();
app.MapM14();
app.MapM15();
app.MapM16();
app.MapM17();
app.MapM19();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet(
        "/api/v1/status",
        () => Results.Ok(new { service = "Berynx Construction 360 API", version = "v1" }))
    .WithName("GetApiStatus");

app.Run();

public partial class Program { }
