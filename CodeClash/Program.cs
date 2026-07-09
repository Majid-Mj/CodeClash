using AspNet.Security.OAuth.GitHub;
using CodeClash.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using CodeClash.API.Middleware;
using CodeClash.Application;
using CodeClash.Infrastructure;
using CodeClash.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Clean Architecture layers ─────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Data Protection Key Persistence ───────────────────────────────────────────
var homePath = Environment.GetEnvironmentVariable("HOME");
var dpFolder = !string.IsNullOrEmpty(homePath)
    ? Path.Combine(homePath, "ASP.NET", "DataProtection-Keys")
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "DataProtection-Keys");

// Ensure the directory exists
Directory.CreateDirectory(dpFolder);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpFolder))
    .SetApplicationName("CodeClash");

// ── 2. JWT Authentication ─────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero   // zero tolerance on token expiry
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = CodeClash.API.Common.ApiResponse<object>.Fail("You are not authorized to access this resource.", "Unauthorized");
            var json = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            await context.Response.WriteAsync(json);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = CodeClash.API.Common.ApiResponse<object>.Fail("You do not have permission to access this resource.", "Forbidden");
            var json = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            await context.Response.WriteAsync(json);
        }
    };
})
.AddCookie()
.AddGitHub(options =>
{
    options.ClientId = builder.Configuration["GitHub:ClientId"]!;
    options.ClientSecret = builder.Configuration["GitHub:ClientSecret"]!;
    options.Scope.Add("user:email");
    options.CallbackPath = "/api/v1/auth/github-callback";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    
    // Enforce secure cookie policies for Azure/reverse proxy compatibility
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.HttpOnly = true;
    options.CorrelationCookie.Path = "/";
    options.CorrelationCookie.IsEssential = true;
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

// SignalR services & Custom Authentication Provider mapping
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// ── 3. Rate Limiting ──────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = CodeClash.API.Common.ApiResponse<object>.Fail("Too many requests. Please try again later.", "Rate limit exceeded");
        var json = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        await context.HttpContext.Response.WriteAsync(json, token);
    };

    options.AddFixedWindowLimiter("register", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("admin-write", opt =>
    {
        opt.Window = TimeSpan.FromHours(1);
        opt.PermitLimit = 100;
        opt.QueueLimit = 0;
    });
});

// ── 4. CORS (Angular dev server) ──────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.SetIsOriginAllowed(origin => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ── 5. Controllers & JSON ─────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        var response = CodeClash.API.Common.ApiResponse<object>.Fail(errors, "Validation failed");
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
    };
});

// ── 6. Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodeClash API",
        Version = "v1",
        Description = "Real-Time Coding Battle Platform — Authentication Endpoints"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if generated (optional)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ── Rate Limiting for OTP/Auth endpoints ──────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("otp", limiterOptions =>
    {
        limiterOptions.PermitLimit = 3;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── SMTP Configuration Validation ─────────────────────────────────────────────
var smtpSection = app.Configuration.GetSection("SmtpSettings");
var smtpUsername = smtpSection["Username"];
var smtpPassword = smtpSection["Password"];
if (string.IsNullOrEmpty(smtpUsername) || smtpUsername.Contains("your-email") ||
    string.IsNullOrEmpty(smtpPassword) || smtpPassword.Contains("your-app-password"))
{
    app.Logger.LogWarning("⚠️  SMTP credentials are not configured. Email features (OTP, verification) will fail at runtime. " +
        "Set SmtpSettings__Username and SmtpSettings__Password environment variables.");
}

// Configure Forwarded Headers for Azure App Service/SSL termination
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// ── 7. Auto-migrate on startup (ensures Azure DB is migrated) ─────────────────
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

try
{
    await db.Database.ExecuteSqlRawAsync(@"
        -- 1. If columns exist but migration is NOT in history, drop columns so migration can recreate them
        IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260703065652_AddPasswordResetOtp')
        BEGIN
            IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PasswordResetOtp')
            BEGIN
                ALTER TABLE Users DROP COLUMN PasswordResetOtp;
            END
            IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PasswordResetToken')
            BEGIN
                ALTER TABLE Users DROP COLUMN PasswordResetToken;
            END
            IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'ResetOtpExpires')
            BEGIN
                ALTER TABLE Users DROP COLUMN ResetOtpExpires;
            END
            IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'ResetTokenExpires')
            BEGIN
                ALTER TABLE Users DROP COLUMN ResetTokenExpires;
            END
        END

        -- 2. If columns are missing but migration IS in history, delete history row so migration will run
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PasswordResetOtp')
        BEGIN
            DELETE FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260703065652_AddPasswordResetOtp';
        END
    ");
}
catch (Exception ex)
{
    Console.WriteLine($"Database self-healing cleanup skipped: {ex.Message}");
}

await db.Database.MigrateAsync();

    // Seed Admin User
    var adminUsername = "Admin123";
    var adminEmail = "admin@codeclash.com";
    var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == adminUsername || u.Email == adminEmail);
    if (adminUser == null)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", workFactor: 12);
        adminUser = CodeClash.Domain.Entities.User.Create(
            "System Administrator",
            adminUsername,
            adminEmail,
            passwordHash
        );
        adminUser.PromoteToAdmin();

        await db.Users.AddAsync(adminUser);
        await db.SaveChangesAsync();
    }
    else if (adminUser.Role != CodeClash.Domain.Enums.UserRole.Admin)
    {
        adminUser.PromoteToAdmin();
        db.Users.Update(adminUser);
        await db.SaveChangesAsync();
    }

// ── 8. Middleware pipeline sd ────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles();

// Enable Swagger in all environments (as required by deployment configuration)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeClash API v1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();