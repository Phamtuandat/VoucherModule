using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DB context
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80); 
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Seed data and migrate database

app.MapGet("/seed", (IHost host) =>
{
    host.EnsureDatabaseMigratedAndSeeded();
    return Results.Ok("Seeded manually");
});

// Login endpoint
app.MapPost("/login", async (
    [FromBody] UserLoginRequest login,
    IUserService userService,
    ITokenService tokenService) =>
{
    Console.WriteLine("➡️  Login endpoint hit");
    Console.WriteLine($"🧾 Username: {login.Username}");

    var user = await userService.GetByUsernameAsync(login.Username);
    if (user is null)
    {
        Console.WriteLine("❌ User not found.");
        return Results.Unauthorized();
    }

    Console.WriteLine("✅ User found in DB. Verifying password...");

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

    return Results.Ok(new { token });
}).AllowAnonymous();


app.Run();
