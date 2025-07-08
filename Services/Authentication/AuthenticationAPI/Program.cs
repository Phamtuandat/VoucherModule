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
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    // log the database connection string for debugging purposes
    Console.WriteLine($"Using connection string: {dbContext.Database.GetDbConnection().ConnectionString}");
    dbContext.Database.Migrate(); // ⚡ Auto apply migrations
}
app.UseAuthentication();
app.UseAuthorization();


// Login endpoint
app.MapPost("/login", async (
    [FromBody] UserLoginRequest login,
    IUserService userService,
    ITokenService tokenService) =>
{
    var user = await userService.GetByUsernameAsync(login.Username);
    if (user is null)
        return Results.Unauthorized();

    var isValid = userService.VerifyPassword(login.Password, user.PasswordHash);
    if (!isValid)
        return Results.Unauthorized();

    var token = tokenService.GenerateToken(
        user.Id.ToString(),
        user.Username,
        user.Role,
        DateTime.UtcNow.AddHours(1));

    return Results.Ok(new { token });
});

app.Run();
