using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VoucherGrpc.Consumer;
using VoucherGrpc.Middlewares;
using VoucherGrpc.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
}); 

builder.Services.AddDbContext<VoucherDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IVoucherTemplateService, VoucherTemplateService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://authenticationapi:80"; // Docker hostname for auth service
        options.RequireHttpsMetadata = false;              // only for dev (Docker)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("VoucherWrite", policy =>
    {
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("voucher:write")));
    });
    options.AddPolicy("VoucherRead", policy =>
    {
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("voucher:read")));
    });

    options.AddPolicy("VoucherApply", policy =>
    {
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("voucher:apply")));
    });

    options.AddPolicy("AdminVoucherWrite", policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(c => c.Type == "scope" && c.Value.Split(' ').Contains("voucher:write")));
    });
});

builder.Services.AddRabbitMqWithConsumers(cfg =>
{
    cfg.AddConsumer<WelcomeVoucherIssueConsumer>();
}, builder.Configuration);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);
});

var app = builder.Build();
// Ensure database is migrated and seeded

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LoggingTokenClaimsMiddleware>();

// Configure the HTTP request pipeline.
app.MapGrpcService<VoucherServiceImpl>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
