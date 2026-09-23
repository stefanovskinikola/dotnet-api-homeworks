using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Loto3000.DataAccess;
using Loto3000.Services;
using Loto3000.Services.DTOs.Auth;
using Loto3000.Web.Infrastructure;
using Loto3000.Web.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.ValidateProductionSecrets();
    // Log to the console and a local daily rolling file. Retention caps file count and age;
    // production deployments should also monitor disk usage and forward logs to durable storage.
    builder.Host.UseSerilog((context, services, logger) => logger
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(Path.Combine(context.HostingEnvironment.ContentRootPath, "Logs", "loto3000-.log"),
            rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30,
            retainedFileTimeLimit: TimeSpan.FromDays(30), shared: true));

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
        options.Limits.MaxRequestBodySize = 32 * 1024;
    });
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
    builder.Services.AddDataAccessLayer(connectionString);
    builder.Services.AddBusinessServices(builder.Configuration);
    builder.Services.AddControllers();
    builder.Services.AddApiRateLimiting();

    var jwt = JwtOptions.FromConfiguration(builder.Configuration);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role",
            ClockSkew = TimeSpan.Zero
        };
    });
    builder.Services.AddAuthorization();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Compiler-generated contracts make endpoint and DTO documentation visible in Swagger.
        foreach (var assemblyName in new[] { "Loto3000.Web", "Loto3000.Services" })
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
        }
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Loto3000 API", Version = "v1", Description = "Play seven numbers, draw eight, and discover the winners." });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Sign in through /api/auth/login, then paste the returned token here (without the Bearer prefix)."
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapControllers();
    // Development enables migrations automatically; production normally deploys schema separately.
    // Seeding is always idempotent and never resets an existing administrator password.
    await app.InitializeDatabaseAsync();
    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Loto3000 terminated unexpectedly.");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
