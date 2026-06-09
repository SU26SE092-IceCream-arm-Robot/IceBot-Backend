using Application;
using Asp.Versioning;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using WebAPI.Authorization;
using WebAPI.Configuration;
using WebAPI.Middlewares;

Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.File("Logs/bootstrap-.txt", rollingInterval: RollingInterval.Day)
                        .CreateBootstrapLogger();

try
{
    Log.Information("Starting up the application...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                                    .AddEnvironmentVariables();

    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>(optional: true);
    }

    builder.Host.UseSerilog(
        (ctx, services, config) =>
            config.ReadFrom.Configuration(ctx.Configuration)
                  .ReadFrom.Services(services),
        writeToProviders: !builder.Environment.IsDevelopment());

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendOnly",
            policy => policy.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader());
    });

    builder.Services.AddOptions<JwtOptions>()
                        .Bind(builder.Configuration.GetSection("Authentication:Jwt"))
                        .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "JWT Secret is required.")
                        .ValidateOnStart();

    var jwt = builder.Configuration.GetSection("Authentication:Jwt").Get<JwtOptions>()!;
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwt.Issuer),
            ValidateAudience = !string.IsNullOrWhiteSpace(jwt.Audience),
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("accounts.manage", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin")));

        options.AddPolicy("payments.manage", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "Manager")));

        options.AddPolicy("products.manage", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "Manager")));

        options.AddPolicy("menus.manage", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "Manager")));

        options.AddPolicy("organizations.manage", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin")));

        options.AddPolicy("organizations.view", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "OrgAdmin")));

        options.AddPolicy("organizations.update", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "OrgAdmin")));

        options.AddPolicy("stores.view", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "OrgAdmin", "Manager")));

        options.AddPolicy("stores.manage", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "OrgAdmin")));

        options.AddPolicy("stores.update", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "OrgAdmin", "Manager")));

        options.AddPolicy("kiosks.view", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "OrgAdmin", "Manager", "Technician")));

        options.AddPolicy("kiosks.manage", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "OrgAdmin", "Manager", "Technician")));

        options.AddPolicy("kiosks.update", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "OrgAdmin", "Manager", "Technician")));

        options.AddPolicy("tenant-tree.view", policy =>
            policy.Requirements.Add(new ScopedRoleRequirement("SystemAdmin", "OrgAdmin", "Manager", "Technician")));
    });

    builder.Services.AddSingleton<IAuthorizationHandler, ScopedRoleAuthorizationHandler>();

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(null, allowIntegerValues: true));
    });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddMvc().AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        const string securitySchemeId = JwtBearerDefaults.AuthenticationScheme;
        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Enter JWT Bearer token **_only_**",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        c.AddSecurityDefinition(securitySchemeId, securityScheme);

        c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(securitySchemeId, document)] = []
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {

    }

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapHealthEndpoints();
    app.MapApplicationInfoEndpoints();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
