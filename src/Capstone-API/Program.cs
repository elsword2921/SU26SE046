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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(ICrudService<>), typeof(CrudService<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailVerificationSender, EmailVerificationSender>();
builder.Services.AddScoped<IDonorRequestService, DonorRequestService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IReceivingOperationsService, ReceivingOperationsService>();
builder.Services.AddScoped<IClassificationOperationsService, ClassificationOperationsService>();
builder.Services.AddScoped<IWarehouseOperationsService, WarehouseOperationsService>();
builder.Services.AddScoped<IManagerDashboardService, ManagerDashboardService>();
builder.Services.AddScoped<IManagerAccountService, ManagerAccountService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddHttpClient<DistributionOperationsService>(client =>
    client.BaseAddress = new Uri("https://dev-online-gateway.ghn.vn/shiip/public-api/"));
builder.Services.AddControllers();
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
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("React",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
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

app.Run();
