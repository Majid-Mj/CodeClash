using AspNet.Security.OAuth.GitHub;
using CodeClash.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using CodeClash.API.Middleware;
using CodeClash.Application;
using CodeClash.Application.Common.Interfaces;
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

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IMatchmakingQueueManager, MatchmakingQueueManager>();
builder.Services.AddScoped<IBattleResolutionService, CodeClash.API.Services.BattleResolutionService>();
builder.Services.AddScoped<ITournamentNotificationService, CodeClash.API.Services.TournamentNotificationService>();
builder.Services.AddScoped<ITournamentMatchRewardService, CodeClash.API.Services.TournamentMatchRewardService>();

var homePath = Environment.GetEnvironmentVariable("HOME");
var dpFolder = !string.IsNullOrEmpty(homePath)
    ? Path.Combine(homePath, "ASP.NET", "DataProtection-Keys")
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "DataProtection-Keys");

// Ensure the directory exists
Directory.CreateDirectory(dpFolder);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpFolder))
    .SetApplicationName("CodeClash");

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

var authBuilder = builder.Services.AddAuthentication(options =>
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

            var response = new { message = "You are not authorized to access this resource.", title = "Unauthorized" };
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

            var response = new { message = "You do not have permission to access this resource.", title = "Forbidden" };
            var json = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            await context.Response.WriteAsync(json);
        }
    };
})
.AddCookie();

if (!string.IsNullOrEmpty(builder.Configuration["GitHub:ClientId"]) &&
    !string.IsNullOrEmpty(builder.Configuration["GitHub:ClientSecret"]))
{
    authBuilder.AddGitHub(options =>
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
}


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

// SignalR services & Custom Authentication Provider mapping
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddScoped<CodeClash.Application.Common.Interfaces.IDuelNotificationService, CodeClash.API.Hubs.DuelNotificationService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new { message = "Too many requests. Please try again later.", title = "Rate limit exceeded" };
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.SetIsOriginAllowed(origin => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

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

        var response = new { message = "Validation failed", errors = errors };
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodeClash API",
        Version = "v1",
        Description = "Real-Time Coding Battle Platform â€” Authentication Endpoints"
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

    options.AddFixedWindowLimiter("AiAnalysisPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(10);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

var smtpSection = app.Configuration.GetSection("SmtpSettings");
var smtpUsername = smtpSection["Username"];
var smtpPassword = smtpSection["Password"];
if (string.IsNullOrEmpty(smtpUsername) || smtpUsername.Contains("your-email") ||
    string.IsNullOrEmpty(smtpPassword) || smtpPassword.Contains("your-app-password"))
{
    app.Logger.LogWarning("âš ï¸  SMTP credentials are not configured. Email features (OTP, verification) will fail at runtime. " +
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

    // Seed Problems
    var adminUserId = adminUser.Id;
    var allowedLangs = "[\"c\", \"cpp\", \"java\", \"csharp\", \"python\", \"javascript\", \"go\", \"rust\"]";

    // Helper to seed a problem if it doesn't exist yet
    async Task SeedProblemAsync(string slug, Func<CodeClash.Domain.Entities.Problem> createFn)
    {
        var exists = await db.Problems.AnyAsync(p => p.Slug == slug);
        if (!exists)
        {
            var problem = createFn();
            await db.Problems.AddAsync(problem);
            await db.SaveChangesAsync();
        }
    }

    // 1. Two Sum
    await SeedProblemAsync("two-sum", () =>
    {
        var p = CodeClash.Domain.Entities.Problem.Create(
            "Two Sum",
            CodeClash.Domain.Enums.Difficulty.Easy,
            CodeClash.Domain.Enums.ProblemCategory.Arrays,
            "Given an array of integers `nums` and an integer `target`, return indices of the two numbers such that they add up to `target`.\n\nYou may assume that each input would have exactly one solution, and you may not use the same element twice.\n\nYou can return the answer in any order.\n\n### Input Format:\n- First line contains the `target` integer.\n- Second line contains space-separated integers representing `nums` array.\n\n### Output Format:\n- Output the two indices separated by a space.",
            "[\"2 <= nums.length <= 10^4\", \"-10^9 <= nums[i] <= 10^9\", \"-10^9 <= target <= 10^9\"]",
            allowedLangs,
            2000,
            256,
            adminUserId
        );
        p.AddTestCase("9\n2 7 11 15", "0 1", false);
        p.AddTestCase("6\n3 2 4", "1 2", false);
        p.AddTestCase("6\n3 3", "0 1", true);
        p.Activate();
        return p;
    });

    // 2. Palindrome Number
    await SeedProblemAsync("palindrome-number", () =>
    {
        var p = CodeClash.Domain.Entities.Problem.Create(
            "Palindrome Number",
            CodeClash.Domain.Enums.Difficulty.Easy,
            CodeClash.Domain.Enums.ProblemCategory.Math,
            "Given an integer `x`, return `true` if `x` is a palindrome, and `false` otherwise.\n\n### Input Format:\n- A single line containing the integer `x`.\n\n### Output Format:\n- Output `true` or `false`.",
            "[\"-2^31 <= x <= 2^31 - 1\"]",
            allowedLangs,
            2000,
            256,
            adminUserId
        );
        p.AddTestCase("121", "true", false);
        p.AddTestCase("-121", "false", false);
        p.AddTestCase("10", "false", true);
        p.Activate();
        return p;
    });

    // 3. Valid Parentheses
    await SeedProblemAsync("valid-parentheses", () =>
    {
        var p = CodeClash.Domain.Entities.Problem.Create(
            "Valid Parentheses",
            CodeClash.Domain.Enums.Difficulty.Easy,
            CodeClash.Domain.Enums.ProblemCategory.Strings,
            "Given a string `s` containing just the characters `(`, `)`, `{`, `}`, `[` and `]`, determine if the input string is valid.\n\nAn input string is valid if:\n1. Open brackets must be closed by the same type of brackets.\n2. Open brackets must be closed in the correct order.\n3. Every close bracket has a corresponding open bracket of the same type.\n\n### Input Format:\n- A single line containing the string `s`.\n\n### Output Format:\n- Output `true` or `false`.",
            "[\"1 <= s.length <= 10^4\", \"s consists of parentheses only '()[]{}'\"]",
            allowedLangs,
            2000,
            256,
            adminUserId
        );
        p.AddTestCase("()", "true", false);
        p.AddTestCase("()[]{}", "true", false);
        p.AddTestCase("(]", "false", false);
        p.AddTestCase("([)]", "false", true);
        p.AddTestCase("{[]}", "true", true);
        p.Activate();
        return p;
    });

    // 4. Longest Substring Without Repeating Characters [Medium]
    await SeedProblemAsync("longest-substring-without-repeating-characters", () =>
    {
        var p = CodeClash.Domain.Entities.Problem.Create(
            "Longest Substring Without Repeating Characters",
            CodeClash.Domain.Enums.Difficulty.Medium,
            CodeClash.Domain.Enums.ProblemCategory.Strings,
            "Given a string `s`, find the length of the longest substring without repeating characters.\n\n### Input Format:\n- A single line containing the string `s`.\n\n### Output Format:\n- A single integer representing the length.",
            "[\"0 <= s.length <= 5 * 10^4\", \"s consists of English letters, digits, symbols and spaces.\"]",
            allowedLangs,
            2000,
            256,
            adminUserId
        );
        p.AddTestCase("abcabcbb", "3", false);
        p.AddTestCase("bbbbb", "1", false);
        p.AddTestCase("pwwkew", "3", true);
        p.Activate();
        return p;
    });

    // 5. Invert Binary Tree [Hard]
    await SeedProblemAsync("invert-binary-tree", () =>
    {
        var p = CodeClash.Domain.Entities.Problem.Create(
            "Invert Binary Tree",
            CodeClash.Domain.Enums.Difficulty.Hard,
            CodeClash.Domain.Enums.ProblemCategory.Trees,
            "Given the root of a binary tree, invert the tree, and return its root.\n\n### Input Format:\n- A single line containing BFS array serialization (e.g., `[4,2,7,1,3,6,9]`).\n\n### Output Format:\n- BFS array serialization of the inverted tree (e.g., `[4,7,2,9,6,3,1]`).",
            "[\"The number of nodes in the tree is in the range [0, 100].\", \"-100 <= Node.val <= 100\"]",
            allowedLangs,
            2000,
            256,
            adminUserId
        );
        p.AddTestCase("[4,2,7,1,3,6,9]", "[4,7,2,9,6,3,1]", false);
        p.AddTestCase("[2,1,3]", "[2,3,1]", false);
        p.AddTestCase("[]", "[]", true);
        p.Activate();
        return p;
    });

    // ── Seed Language Templates (idempotent) ─────────────────────────────────
    // Insert wrapper templates for all existing problems if they don't exist yet.
    // This block runs on every startup and is safe to re-run (checks for existing records).
    var problemsNeedingTemplates = await db.Problems
        .Include(p => p.LanguageTemplates)
        .Where(p => p.DeletedAt == null)
        .ToListAsync();

    foreach (var prob in problemsNeedingTemplates)
    {
        var templates = CodeClash.Infrastructure.Seeding.ProblemTemplateSeeder.GetTemplates(prob.Slug);
        if (templates == null) continue;

        foreach (var (lang, wrapper, starter) in templates)
        {
            var existing = prob.LanguageTemplates.FirstOrDefault(t =>
                t.Language.Equals(lang, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                prob.AddLanguageTemplate(lang, wrapper, starter);
            }
            else
            {
                existing.Update(wrapper, starter);
            }
        }
    }

    await db.SaveChangesAsync();

// ── 8. Middleware pipeline ────────────────────────────────────────────────────
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
app.MapHub<MatchmakingHub>("/hubs/matchmaking");
app.MapHub<BattleHub>("/hubs/battle");
app.MapHub<TournamentHub>("/hubs/tournament");

app.Run();
