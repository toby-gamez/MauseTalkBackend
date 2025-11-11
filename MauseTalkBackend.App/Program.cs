using MauseTalkBackend.Api.Data;
using MauseTalkBackend.Api.Repositories;
using MauseTalkBackend.Api.Services;
using MauseTalkBackend.App.Hubs;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MauseTalkBackend API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your JWT token in the text input below.\n\nExample: \"Bearer 12345abcdef\""
    });
    
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5261", 
                "https://localhost:5261",
                "https://localhost:7189"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MauseTalkDbContext>(options =>
{
    if (connectionString?.Contains("Server=") == true)
    {
        // SQL Server
        options.UseSqlServer(connectionString);
    }
    else
    {
        // SQLite fallback
        options.UseSqlite(connectionString ?? "Data Source=mausetalk.db");
    }
});

// Add repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IInviteLinkRepository, InviteLinkRepository>();

// Add services
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<AuthService>();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32Characters!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "MauseTalkBackend",
            ValidAudience = jwtSettings["Audience"] ?? "MauseTalkBackend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ApiConstants.Policies.RequireAuthenticated, policy =>
        policy.RequireAuthenticatedUser());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply database migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MauseTalkDbContext>();
    context.Database.Migrate();
    
    // Seed test user
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    try
    {
        var testUser = new MauseTalkBackend.Domain.DTOs.CreateUserDto
        {
            Username = "testuser",
            Email = "test@example.com", 
            Password = "password123",
            DisplayName = "Test User"
        };
        
        // Check if user already exists
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var existingUser = await userRepo.GetByUsernameAsync("testuser");
        
        if (existingUser == null)
        {
            await authService.RegisterAsync(testUser);
            Console.WriteLine("✅ Test user 'testuser' created successfully!");
        }
        else
        {
            Console.WriteLine("ℹ️ Test user 'testuser' already exists");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error creating test user: {ex.Message}");
    }
}

// app.UseHttpsRedirection(); // Disabled for development

app.UseCors(); // CORS must be before authentication

app.UseStaticFiles();

// Serve uploaded files
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hub/chat");

// Add root endpoint
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();