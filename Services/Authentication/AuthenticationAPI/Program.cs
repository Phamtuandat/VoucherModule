using AuthenticationAPI.Data.DatabaseExtensions;
using AuthenticationAPI.Models;
using AuthenticationAPI.Services;
using BuildingBlocks.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🧱 Configure EF Core with SQL Server
builder.Services.AddScoped<UserCreatedInterceptor>();
builder.Services.AddDbContext<AuthDbContext>((serviceProvider, options) =>
    {
        
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        options.AddInterceptors(serviceProvider.GetRequiredService<UserCreatedInterceptor>());
    }
);

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
            ValidIssuer = jwtSettings?.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings?.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.Key ?? "developemt-test-key")),
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

// Add masstransit from BuildingBlocks.Messaging package
builder.Services.AddRabbitMqWithConsumers(cfg =>{}, builder.Configuration);
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

// Seed endpoint for development/testing
app.MapGet("/seed", async (IHost host) =>
{
    await host.EnsureDatabaseMigratedAndSeeded();
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var users = await db.Users.ToListAsync();
    return Results.Ok(users);
}).AllowAnonymous();
// Clear seed endpoint for development/testing
app.MapGet("/clearSeed", async (IHost host) =>
{
    await host.ClearSeedData();
    return Results.Ok("Seed data has been cleared.");
}).AllowAnonymous();

// 🔑 Authentication endpoints
// Login endpoint
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
// Register endpoint
app.MapPost("/register", async (
    [FromBody] UserRegisterRequest model,
    IUserService userService,
    ITokenService tokenService) =>
{
    if (model == null)
        return Results.BadRequest("Invalid payload");

    // Check if user already exists
    var existingUser = await userService.GetByUsernameAsync(model.Username);
    if (existingUser != null)
        return Results.Conflict("Username already exists");

    // Map and create
    var user = UserMapper.ToEntity(model);
    var createdUser = await userService.CreateAsync(user, model.Password);

    // Optional: generate token after registration
    var token = tokenService.GenerateToken(createdUser.Id.ToString(), createdUser.Username, createdUser.Role, DateTime.UtcNow.AddHours(1));

    return Results.Ok(new
    {
        Message = "Registration successful",
        User = new
        {
            createdUser.Id,
            createdUser.Username,
            createdUser.Email,
            createdUser.Role
        },
        Token = token
    });
}).AllowAnonymous();
// Refresh token endpoint
app.MapPost("/refresh", async (
    [FromBody] RefreshTokenRequest req,
    IUserService userService,
    ITokenService tokenService) =>
{
    if (string.IsNullOrWhiteSpace(req.RefreshToken))
        return Results.BadRequest("Refresh token is required.");

    var storedToken = await userService.GetValidRefreshTokenAsync(req.RefreshToken);

    if (storedToken == null ||
        storedToken.IsUsed ||
        storedToken.IsRevoked ||
        storedToken.ExpiryDate <= DateTime.UtcNow)
    {
        return Results.Unauthorized(); // Token invalid or expired
    }

    // Invalidate the used token (prevent reuse)
    await userService.InvalidateRefreshTokenAsync(storedToken.Token);

    // Fetch user from token
    var user = storedToken.User;

    // Generate new tokens
    var newAccessToken = tokenService.GenerateToken(
        user.Id.ToString(),
        user.Username,
        user.Role,
        DateTime.UtcNow.AddMinutes(15) // Access token lifetime
    );

    var newRefreshToken = await userService.CreateRefreshTokenAsync(user);

    return Results.Ok(new
    {
        accessToken = newAccessToken,
        refreshToken = newRefreshToken.Token
    });
}).AllowAnonymous();

// 🧑‍🤝‍🧑 User management endpoints
app.MapGet("/users", async (IUserService userService) =>
{
    var users = await userService.GetAllUsersAsync();
    return Results.Ok(users);
}).RequireAuthorization("ReadUsersScope");

// 🏁 Start application
app.Run();
