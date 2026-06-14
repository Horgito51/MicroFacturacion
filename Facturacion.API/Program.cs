using System.Text;
using System.Security.Claims;
using System.Text.Json;
using Facturacion.API.Eventing;
using Asp.Versioning;
using Facturacion.API.Models.Settings;
using Facturacion.API.Services;
using Facturacion.API.GrpcServices;
using Facturacion.Business.Interfaces.Facturacion;
using Facturacion.Business.Services.Facturacion;
using Facturacion.DataAccess.Context;
using Facturacion.DataAccess.Repositories.Facturacion;
using Facturacion.DataAccess.Repositories.Interfaces.Facturacion;
using Facturacion.DataManagement.Facturacion.Interfaces;
using Facturacion.DataManagement.Facturacion.Services;
using Facturacion.DataManagement.Eventing.Interfaces;
using Facturacion.DataManagement.Eventing.Services;
using Facturacion.DataManagement.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

const string LocalCorsPolicy = "LocalCorsPolicy";

string[] backOfficeRoles =
[
    "ADMINISTRADOR",
    "ADMIN",
    "RECEPCIONISTA",
    "OPERATIVO",
    "DESK_SERVICE"
];

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
ConfigureHttpEndpoint(builder);
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddHealthChecks();
var disableAuthorizationForTesting = builder.Configuration.GetValue<bool>("Security:DisableAuthorizationForTesting");
var connectionString = ResolveDefaultConnectionString(builder.Configuration, builder.Environment);

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Microservicio Facturacion API", Version = "v1" });
    if (!disableAuthorizationForTesting)
    {
        AddBearerSecurity(options);
        options.OperationFilter<AuthorizeOperationFilter>();
    }
});
builder.Services.AddCors(options => options.AddPolicy(LocalCorsPolicy, policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddDbContext<FacturacionDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddOptions<PaymentGatewaySettings>().BindConfiguration("PaymentGateway");
builder.Services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
builder.Services.AddScoped<IFacturaDetalleRepository, FacturaDetalleRepository>();
builder.Services.AddScoped<IPagoRepository, PagoRepository>();
builder.Services.AddScoped<IFacturaDataService, FacturaDataService>();
builder.Services.AddScoped<IPagoDataService, PagoDataService>();
builder.Services.AddScoped<IOutboxMessageService, OutboxMessageService>();
builder.Services.AddScoped<IInboxMessageService, InboxMessageService>();
builder.Services.AddScoped<IFacturaService, FacturaService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddScoped<IEventBus, RabbitMqEventBus>();
builder.Services.AddHostedService<OutboxPublisherHostedService>();
builder.Services.AddHostedService<ReservaConfirmadaConsumerHostedService>();
AddJwtAuthentication(builder.Services, builder.Configuration, builder.Environment, backOfficeRoles);
if (disableAuthorizationForTesting)
{
    builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, TestingAuthorizationMiddlewareResultHandler>();
}

var app = builder.Build();
app.Logger.LogInformation("Iniciando {Service} en ambiente {Environment}", "Microservicio.Facturacion", app.Environment.EnvironmentName);
app.Logger.LogInformation("Swagger habilitado en /swagger y /swagger/v1/swagger.json");
app.Logger.LogInformation("Cadena de conexion DefaultConnection cargada para {Service}", "Microservicio.Facturacion");
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Microservicio Facturacion API v1"));
app.UseMiddleware<Facturacion.API.Middleware.ExceptionHandlingMiddleware>();
if (app.Configuration.GetValue("HttpsRedirection:Enabled", !app.Environment.IsDevelopment()))
{
    app.UseHttpsRedirection();
}
app.UseCors(LocalCorsPolicy);
app.UseAuthentication();
app.UseMiddleware<AdminProfileAccessMiddleware>();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Microservicio.Facturacion" }));
app.MapGet("/health/rabbitmq", (RabbitMqConnection rabbitMqConnection) =>
{
    var canConnect = rabbitMqConnection.CanConnect();
    return canConnect
        ? Results.Ok(new { status = "ok", broker = "connected", service = "Microservicio.Facturacion" })
        : Results.Problem("No fue posible conectar a RabbitMQ desde Facturacion.", statusCode: StatusCodes.Status503ServiceUnavailable);
});
app.MapGet("/health/db", async (FacturacionDbContext dbContext, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("HealthDb");
    try
    {
        logger.LogInformation("Validando conexion a base de datos de Facturacion");
        var canConnect = await dbContext.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "ok", database = "connected", service = "Microservicio.Facturacion" })
            : Results.Problem("No fue posible conectar a la base de datos de Facturacion.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error validando conexion a base de datos de Facturacion");
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapControllers();
app.MapGrpcService<FacturacionGrpcService>();
app.MapGrpcService<PagoGrpcService>();
app.Logger.LogInformation("{Service} listo para recibir solicitudes", "Microservicio.Facturacion");
app.Run();

static void ConfigureHttpEndpoint(WebApplicationBuilder builder)
{
    var configuredPort = builder.Configuration.GetValue<int?>("Ports:Http");
    if (configuredPort is null)
        return;

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(configuredPort.Value, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });
    });
}

static string ResolveDefaultConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
{
    var configuredConnectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(configuredConnectionString))
        throw new InvalidOperationException("La cadena de conexion 'DefaultConnection' es obligatoria.");

    if (HasPassword(configuredConnectionString))
        return configuredConnectionString;

    var fileConfiguration = new ConfigurationBuilder()
        .SetBasePath(environment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .Build();

    var fileConnectionString = fileConfiguration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(fileConnectionString) && HasPassword(fileConnectionString))
        return fileConnectionString;

    throw new InvalidOperationException(
        "La cadena de conexion 'DefaultConnection' esta incompleta: falta Password/Pwd. Revise la configuracion del App Service o appsettings.json.");
}

static bool HasPassword(string connectionString)
{
    try
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        return !string.IsNullOrWhiteSpace(builder.Password);
    }
    catch (ArgumentException)
    {
        return connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Pwd=", StringComparison.OrdinalIgnoreCase);
    }
}

static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment, string[] backOfficeRoles)
{
    var secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("La configuracion 'Jwt:Secret' es obligatoria.");
    var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("La configuracion 'Jwt:Issuer' es obligatoria.");
    var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("La configuracion 'Jwt:Audience' es obligatoria.");

    if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        throw new InvalidOperationException("La configuracion 'Jwt:Secret' debe tener al menos 32 caracteres.");

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");
                logger.LogWarning("JWT auth failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "No autorizado. Se requiere token de autenticacion valido.",
                        statusCode = StatusCodes.Status401Unauthorized,
                        errors = (object?)null,
                        traceId = context.HttpContext.TraceIdentifier,
                        timestamp = DateTime.UtcNow
                    }));
                }

                return Task.CompletedTask;
            }
        };
    });

    services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminProfile", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => !context.User.IsInRole("CLIENTE"));
        });

        options.AddPolicy("BackOffice", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(backOfficeRoles);
        });
    });
}
static void AddBearerSecurity(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header, Description = "Ingrese el token JWT con el formato: Bearer {token}" });
}

sealed class AuthorizeOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return;

        var hasAllowAnonymous = descriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()
            || descriptor.MethodInfo.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any()
            || descriptor.ControllerTypeInfo.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any();

        if (hasAllowAnonymous)
            return;

        var hasAuthorize = descriptor.EndpointMetadata.OfType<IAuthorizeData>().Any()
            || descriptor.MethodInfo.GetCustomAttributes(true).OfType<IAuthorizeData>().Any()
            || descriptor.ControllerTypeInfo.GetCustomAttributes(true).OfType<IAuthorizeData>().Any();

        if (!hasAuthorize)
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
        });
    }
}

sealed class TestingAuthorizationMiddlewareResultHandler(IConfiguration configuration) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (configuration.GetValue<bool>("Security:DisableAuthorizationForTesting"))
            return next(context);

        return _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
sealed class AdminProfileAccessMiddleware(RequestDelegate next)
{
    private static readonly string[] BackOfficeRoles =
    [
        "ADMINISTRADOR",
        "ADMIN",
        "RECEPCIONISTA",
        "OPERATIVO",
        "DESK_SERVICE"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresAdminProfile(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "No autorizado. Se requiere autenticacion.");
            return;
        }

        var roles = context.User.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();

        var isCliente = roles.Any(role => string.Equals(role, "CLIENTE", StringComparison.OrdinalIgnoreCase));
        var hasBackOfficeRole = roles.Any(role =>
            BackOfficeRoles.Any(allowed => string.Equals(role, allowed, StringComparison.OrdinalIgnoreCase)));

        if (!hasBackOfficeRole)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "Acceso denegado. Se requiere rol administrativo o de recepcion.");
            return;
        }

        if (isCliente)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "Acceso denegado. El rol CLIENTE no puede ingresar al perfil administrativo.");
            return;
        }

        await next(context);
    }

    private static bool RequiresAdminProfile(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.Contains("/internal/", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("/internal/auth/", StringComparison.OrdinalIgnoreCase);
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            message,
            statusCode,
            errors = (object?)null,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        }));
    }
}
