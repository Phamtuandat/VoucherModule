using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🧱 Configure EF Core with SQL Server
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔐 Bind and configure JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // should be true in production
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // precise expiration
        };
    });

// 🔐 Authorization service
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadUsersScope", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var scopeClaim = context.User.FindFirst("scope")?.Value;
            return scopeClaim?.Split(' ').Contains("read:users") ?? false;
        });
    });

});


// 🔧 Dependency Injection
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ⚙️ Listen on port 80 inside container or Kestrel host
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);
});

var app = builder.Build();

// 🌐 Middleware pipeline
app.UseAuthentication();
app.UseAuthorization();

// 🧪 Seed endpoint for development/testing
app.MapGet("/seed", async (IHost host) =>
{
    await host.EnsureDatabaseMigratedAndSeeded();
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var users = await db.Users.ToListAsync();
    return Results.Ok(users);
}).AllowAnonymous();

app.MapGet("/clearSeed", async (IHost host) =>
{
    await host.ClearSeedData();
    return Results.Ok("Seed data has been cleared.");
}).AllowAnonymous();


app.MapPost("/login", async (
    [FromBody] UserLoginRequest login,
    IUserService userService,
    ITokenService tokenService) =>
{
    Console.WriteLine("➡️ Login endpoint hit");
    Console.WriteLine($"🧾 Username: {login.Username}");

    var user = await userService.GetByUsernameAsync(login.Username);
    if (user is null)
    {
        Console.WriteLine("❌ User not found.");
        return Results.Unauthorized();
    }

    var isValid = userService.VerifyPassword(login.Password, user.PasswordHash);
    if (!isValid)
    {
        Console.WriteLine("❌ Invalid password.");
        Console.WriteLine(login.Password, user.PasswordHash);

        return Results.Unauthorized();
    }

    Console.WriteLine("✅ Password valid. Generating token...");

    var token = tokenService.GenerateToken(
        user.Id.ToString(),
        user.Username,
        user.Role,
        DateTime.UtcNow.AddHours(1));

    Console.WriteLine("✅ Token generated successfully.");

    return Results.Json(new { token });
}).AllowAnonymous();
app.MapGet("/users", async (IUserService userService) =>
{
    var users = await userService.GetAllUsersAsync();
    return Results.Ok(users);
}).RequireAuthorization("ReadUsersScope");

// 🏁 Start application
app.Run();
