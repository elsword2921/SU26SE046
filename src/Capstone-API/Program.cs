using BLL.Services.Implements.AuthService;
using BLL.Services.Implements.Common;
using BLL.Services.Implements.DonorRequestService;
using BLL.Services.Implements.WarehouseService;
using BLL.Services.Implements.ReceivingOperations;
using BLL.Services.Interfaces.AuthService;
using BLL.Services.Interfaces.Common;
using BLL.Services.Interfaces.DonorRequestService;
using BLL.Services.Interfaces.WarehouseService;
using BLL.Services.Interfaces.ReceivingOperations;
using BLL.Services.Implements.ClassificationOperations;
using BLL.Services.Interfaces.ClassificationOperations;
using BLL.Services.Implements.WarehouseOperations;
using BLL.Services.Interfaces.WarehouseOperations;
using BLL.Services.Implements.ManagerDashboard;
using BLL.Services.Interfaces.ManagerDashboard;
using BLL.Services.Implements.ManagerAccounts;
using BLL.Services.Interfaces.ManagerAccounts;
using BLL.Services.Implements.DistributionOperations;
using BLL.Services.Interfaces.Voucher;
using BLL.Services.Implements.Voucher;
using DAL;
using DAL.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Authentication;
using Microsoft.OpenApi;
using System.Text;
using Capstone_API.Hubs;
using Capstone_API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<ShiftLifecycleWorker>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(ICrudService<>), typeof(CrudService<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailVerificationSender, EmailVerificationSender>();
builder.Services.AddHttpClient<DonorRequestService>(client =>
{
    client.BaseAddress = new Uri("https://api.geoapify.com/");
});
builder.Services.AddScoped<IDonorRequestService>(provider =>
    provider.GetRequiredService<DonorRequestService>());
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IReceivingOperationsService, ReceivingOperationsService>();
builder.Services.AddScoped<IClassificationOperationsService, ClassificationOperationsService>();
builder.Services.AddHttpClient<GeminiClassificationService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(90);
});
builder.Services.AddScoped<IWarehouseOperationsService, WarehouseOperationsService>();
builder.Services.AddScoped<IManagerDashboardService, ManagerDashboardService>();
builder.Services.AddScoped<IManagerAccountService, ManagerAccountService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddHttpClient<DistributionOperationsService>(client =>
    client.BaseAddress = new Uri("https://dev-online-gateway.ghn.vn/shiip/public-api/"));
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste JWT token here"
        });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services
.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer =
                builder.Configuration["Jwt:Issuer"],

            ValidAudience =
                builder.Configuration["Jwt:Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        builder.Configuration["Jwt:Key"]!))
        };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                context.Token = token;
            return Task.CompletedTask;
        }
    };
});

var configuredCorsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
var environmentCorsOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var allowedCorsOrigins = configuredCorsOrigins
    .Concat(environmentCorsOrigins)
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

static bool IsCorsOriginAllowed(string origin, IReadOnlyCollection<string> allowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var requestOrigin)) return false;

    foreach (var allowedOrigin in allowedOrigins)
    {
        if (!allowedOrigin.Contains('*'))
        {
            if (string.Equals(origin.TrimEnd('/'), allowedOrigin, StringComparison.OrdinalIgnoreCase))
                return true;
            continue;
        }

        if (!Uri.TryCreate(allowedOrigin.Replace("*.", "wildcard."), UriKind.Absolute,
                out var wildcardOrigin)) continue;
        var hostSuffix = wildcardOrigin.Host["wildcard".Length..];
        if (requestOrigin.Scheme.Equals(wildcardOrigin.Scheme, StringComparison.OrdinalIgnoreCase)
            && requestOrigin.Host.EndsWith(hostSuffix, StringComparison.OrdinalIgnoreCase)
            && requestOrigin.Host.Length > hostSuffix.Length)
            return true;
    }

    return false;
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("React", policy => policy
        .SetIsOriginAllowed(origin => IsCorsOriginAllowed(origin, allowedCorsOrigins))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (UnauthorizedAccessException exception)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = exception.Message });
    }
    catch (AuthenticationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = exception.Message });
    }
});

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    // Keep the JWT in Swagger UI when the document/page is refreshed.
    // Without this, Swagger can silently stop attaching Authorization after a UI reload,
    // making every protected endpoint return 401 until the token is entered again.
    options.EnablePersistAuthorization();
});

app.UseHttpsRedirection();

app.UseCors("React");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHub<DonationChatHub>("/hubs/donation-chat");

app.Run();
