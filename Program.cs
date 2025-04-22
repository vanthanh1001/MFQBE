using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Google.Apis.Auth.OAuth2;
using Models.Interfaces;
using Services.Implementations;
using Services.Interfaces;
using FitnessApp.API.Models;
using FitnessApp.API.Middleware;
using System.Security.Claims;
using Services.Interfaces;
using FitnessApp.API.Services.Interfaces;
using FitnessApp.API.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Cấu hình Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fitness API", Version = "v1" });
    
    // Thêm cấu hình bảo mật cho Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Đăng ký Repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Đăng ký AuthService
// builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();

// Add PayOS service
// Update the PayOS service registration
builder.Services.AddScoped<IPaymentService, PayOSService>();
builder.Services.AddHttpClient<PayOSService>();

builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
// Thêm service mới
builder.Services.AddSingleton<FirebaseInitializationService>();
builder.Services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();

// Cấu hình Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://securetoken.google.com/mfquest-b89b0";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = "https://securetoken.google.com/mfquest-b89b0",
            ValidAudience = "mfquest-b89b0",
            ValidateIssuerSigningKey = true,
            // Thêm cấu hình chi tiết cho xác thực
            ClockSkew = TimeSpan.Zero // Giảm độ trễ
        };
        
        // Bật để xem chi tiết lỗi token
        options.IncludeErrorDetails = true;
        
        // Thêm event handlers để ghi log quá trình xác thực
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                Console.WriteLine($"Authentication failed details: {context.Exception}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token được xác thực thành công");
                // Đảm bảo ClaimTypes.NameIdentifier luôn có trong token
                var identity = context.Principal.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    // Lấy UID từ token Firebase
                    var uidClaim = identity.FindFirst("user_id") ?? identity.FindFirst("sub");
                    if (uidClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                    {
                        // Thêm claim chuẩn NameIdentifier nếu chưa có
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, uidClaim.Value));
                        Console.WriteLine($"Added NameIdentifier claim: {uidClaim.Value}");
                    }
                }
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                Console.WriteLine($"Received token: {context.Token?.Substring(0, Math.Min(10, context.Token?.Length ?? 0))}...");
                return Task.CompletedTask;
            }
        };
    });

// Cấu hình Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI(c =>
//     {
//         c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fitness API V1");
//         c.RoutePrefix = "swagger";
//         c.EnableDeepLinking();
//         c.DisplayRequestDuration();
//         c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
//         c.EnableFilter();
//         c.EnableTryItOutByDefault();
//     });
// }
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fitness API V1");
    c.RoutePrefix = "swagger";
    c.EnableDeepLinking();
    c.DisplayRequestDuration();
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    c.EnableFilter();
    c.EnableTryItOutByDefault();
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Static files middleware
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "index.html" }
});
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health-check", () => "Application is running");

try
{
    // Initialize Firebase BEFORE using any Firebase services
    var firebaseInit = app.Services.GetRequiredService<FirebaseInitializationService>();
    firebaseInit.Initialize();
    Console.WriteLine("Firebase initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Firebase initialization error: {ex.Message}");
    // Tiếp tục chạy ứng dụng ngay cả khi Firebase khởi tạo thất bại
}

// Add middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.Run();