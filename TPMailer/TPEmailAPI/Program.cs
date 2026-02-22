using TPEmail.DataAccess.Interface.Service.v1_0;
using TPEmail.DataAccess.Service.v1_0;
using TPEmail.DataAccess.Interface.Repository.v1_0;
using TPEmail.DataAccess.Repository.v1_0;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TPEmailAPI;
using NLog;
using NLog.Web;
using TPEmailAPI.Middleware;
using Asp.Versioning;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using TPEmail.Common.Helpers;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

    // Database Configuration
    // SQL Server connection string for TPMailer database
    Environment.SetEnvironmentVariable("tpsqlconnection", "");
    
    // JWT Configuration
    // Secret key used to sign and validate JWT tokens
    Environment.SetEnvironmentVariable("tpjwtkey", "");
    // JWT token issuer (API base URL)
    Environment.SetEnvironmentVariable("tpjwtissuer", "https://localhost:7187/");
    // JWT token audience (frontend base URL)
    Environment.SetEnvironmentVariable("tpjwtaudience", "http://localhost:7188/");
    
    // Logging Configuration
    // Elmah.io API key for cloud error logging
    Environment.SetEnvironmentVariable("tpelmahapikey", "");
    // Elmah.io log ID (GUID) for cloud error logging
    Environment.SetEnvironmentVariable("tpelmahlogid", "");
    
    // External SMTP Configuration
    // SMTP username for sending emails via external provider
    Environment.SetEnvironmentVariable("tpsmtpuser", "");
    // SMTP app password for external email provider
    Environment.SetEnvironmentVariable("tpsmtpsecret", "f");
    // SMTP port number for external email provider
    Environment.SetEnvironmentVariable("tpsmtpport", "");
    // SMTP host address for external email provider
    Environment.SetEnvironmentVariable("tpemailtpsmtp", "s");
    // SendGrid API key for email delivery via SendGrid
    Environment.SetEnvironmentVariable("tpsendgridapikey", "");
    
    // Attachment Configuration
    // Maximum email attachment size in megabytes
    Environment.SetEnvironmentVariable("tpmaxattachmentsizemb", "");
    
    // Internal SMTP Relay Configuration
    // Default sender email address for system-generated emails (Welcome, Guidance)
    Environment.SetEnvironmentVariable("tpinternalfromemail", "");
    
    // Exchange/O365 Configuration
    // Exchange Web Services (EWS) endpoint URL for O365/Exchange email sending
    Environment.SetEnvironmentVariable("tpemailtestes", "https://outlook.office365.com/EWS/Exchange.asmx");

    // Microsoft Graph API Configuration
    // Azure AD client ID for Microsoft Graph API authentication
    Environment.SetEnvironmentVariable("tpgraphclientid", "");
    // Azure AD client secret for Microsoft Graph API authentication
    Environment.SetEnvironmentVariable("tpgraphclientsecret", "");
    // Azure AD tenant ID for Microsoft Graph API authentication
    Environment.SetEnvironmentVariable("tpgraphtenantid", "");
    // Microsoft Graph API base URL
    Environment.SetEnvironmentVariable("tpgraphbaseurl", "https://graph.microsoft.com/v1.0");
    // OAuth scope for Microsoft Graph API token requests
    Environment.SetEnvironmentVariable("tpgraphoauthscope", "https://graph.microsoft.com/.default");
    // Azure AD OAuth token endpoint base URL
    Environment.SetEnvironmentVariable("tpgraphtokenendpoint", "https://login.microsoftonline.com");
    // Comma-separated user profile fields to fetch from Graph API
    Environment.SetEnvironmentVariable("tpgraphuserfields", "id,displayName,givenName,surname,userPrincipalName,mail,jobTitle,department,officeLocation,mobilePhone,companyName");

    // CSP Configuration
    // API base URL used in Content-Security-Policy connect-src directive
    Environment.SetEnvironmentVariable("tpcspconnectsrc", "https://localhost:7187");

    // Security / Hashing Configuration
    // Salt size in bytes for PBKDF2 and Argon2 password hashing
    Environment.SetEnvironmentVariable("tphashsaltsize", "32");
    // Hash output size in bytes for PBKDF2 and Argon2 key derivation
    Environment.SetEnvironmentVariable("tphashoutputsize", "64");
    // Number of iterations for PBKDF2-SHA512 key derivation
    Environment.SetEnvironmentVariable("tppbkdf2iterations", "100000");
    // Argon2id degree of parallelism (number of threads)
    Environment.SetEnvironmentVariable("tpargonparallelism", "8");
    // Argon2id number of iterations (time cost)
    Environment.SetEnvironmentVariable("tpargontimecost", "4");
    // Argon2id memory usage in KB
    Environment.SetEnvironmentVariable("tpargonmemorysize", "2048");
    // Allowed characters for random string generation (tokens, secrets)
    Environment.SetEnvironmentVariable("tprandomcharset", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");

    // Email Composition Configuration
    // Default sender display name for system-generated emails
    Environment.SetEnvironmentVariable("tpemailsendername", "TPMailer");
    // Default email service ID used for user registration emails
    Environment.SetEnvironmentVariable("tpdefaultemailserviceid", "3");

    // Refresh Token Cleanup Configuration
    // Initial delay in minutes before first expired token cleanup runs
    Environment.SetEnvironmentVariable("tptokencleanupdelaymin", "5");
    // Interval in hours between expired token cleanup cycles
    Environment.SetEnvironmentVariable("tptokencleanupintervalhrs", "24");

    ConfigureNLog(builder.Configuration);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers();
    
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;  
        options.ReportApiVersions = true;
        options.ApiVersionReader = new HeaderApiVersionReader("api-version");
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = false;
    });
    
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddMemoryCache();
    builder.Services.AddHttpContextAccessor();

    var connectionString = Environment.GetEnvironmentVariable("tpsqlconnection")
        ?? builder.Configuration.GetConnectionString("SqlConnection")
        ?? throw new InvalidOperationException("SQL connection string not configured.");
    Func<IDbConnection> tpmailerdb = () => new SqlConnection(connectionString);
    builder.Services.AddSingleton(tpmailerdb);

    builder.Services.AddHttpClient();

    // Repositories
    builder.Services.AddScoped<IAppRepository, AppRepository>();
    builder.Services.AddScoped<IEmailRepository, EmailRepository>();
    builder.Services.AddScoped<IAuthRepository, AuthRepository>();

    // Primary Services
    builder.Services.AddScoped<IAppService, AppService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    builder.Services.AddHostedService<RefreshTokenCleanupService>();

    // Interface aliases (resolved from the same AppService/EmailService scoped instances)
    builder.Services.AddScoped<IJWTToken>(sp => sp.GetRequiredService<IAppService>());
    builder.Services.AddScoped<ICommonData>(sp => sp.GetRequiredService<IAppService>());
    builder.Services.AddScoped<IActivityLog>(sp => sp.GetRequiredService<IAppService>());
    builder.Services.AddScoped<IErrorLog>(sp => sp.GetRequiredService<IAppService>());
    builder.Services.AddScoped<IAppLookup>(sp => sp.GetRequiredService<IAppService>());
    builder.Services.AddScoped<IEmailServiceLookup>(sp => sp.GetRequiredService<IAppService>());
    builder.Services.AddScoped<IUserService>(sp => sp.GetRequiredService<IAppService>());
    builder.Services.AddScoped<IEmailRecipients>(sp => sp.GetRequiredService<IEmailService>());

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TP Mailer API",
            Version = "v1",
            Description = "TP Mailer Email Service "
        });

        c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme."
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "bearer"
                    }
                },
                new string[] {}
            }
        });
    });

    #region JWT Authentication
    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtKey = Environment.GetEnvironmentVariable("tpjwtkey")!;
        var jwtIssuer = Environment.GetEnvironmentVariable("tpjwtissuer");
        var jwtAudience = Environment.GetEnvironmentVariable("tpjwtaudience");

        var Key = Encoding.UTF8.GetBytes(jwtKey);
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Key)
        };
    });
    #endregion

    builder.Services.AddElmahIo(o =>
    {
        o.ApiKey = Environment.GetEnvironmentVariable("tpelmahapikey");
        var logId = Environment.GetEnvironmentVariable("tpelmahlogid");
        if (!string.IsNullOrWhiteSpace(logId))
        {
            o.LogId = new Guid(logId);
        }
    });

    // ── Strict CORS Policy ─────────────────────────────────────────────
   var allowedOrigins = new[] { "http://localhost:4200", "https://localhost:4200" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("StrictCorsPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    AppConfig.Initialize(builder.Configuration);

    var vaultUrl = Environment.GetEnvironmentVariable("tpkeyvaulturl");
    var vaultClientId = Environment.GetEnvironmentVariable("tpkeyvaultclientid");
    var vaultSecret = Environment.GetEnvironmentVariable("tpkeyvaultclientsecret");
    var vaultTenantId = Environment.GetEnvironmentVariable("tpkeyvaulttenantid");
    if (!string.IsNullOrWhiteSpace(vaultUrl) && !string.IsNullOrWhiteSpace(vaultClientId)
        && !string.IsNullOrWhiteSpace(vaultSecret) && !string.IsNullOrWhiteSpace(vaultTenantId))
    {
        KeyVaultHelper.Initialize(vaultUrl, vaultClientId, vaultSecret, vaultTenantId);
    }

    var keyConfigProvider = new KeyConfigProvider(tpmailerdb);
    EncryptionHelper.Initialize(keyConfigProvider);

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();

    app.UseSecurityHeaders();

    app.UseCors("StrictCorsPolicy");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options => {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TP Mailer API v1");
            options.DisplayOperationId();
            options.RoutePrefix = string.Empty;
        });
    }

    app.UseElmahIo();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}

static void ConfigureNLog(IConfiguration configuration)
{
    var connectionString = Environment.GetEnvironmentVariable("tpsqlconnection")
        ?? configuration.GetConnectionString("SqlConnection");
    LogManager.Configuration!.Variables["connectionString"] = connectionString!;
}

