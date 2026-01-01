using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using test_shopify_app.appDataBase;
using test_shopify_app.DTOmapper;
using test_shopify_app.Services;
using test_shopify_app.Token;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ----------------------------
// Add services to the container
// ----------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ Enable CORS for React frontend (localhost:5175)
var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ✅ Database Context
builder.Services.AddDbContext<AppDBcontext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ AutoMapper and Services
builder.Services.AddAutoMapper(typeof(DtoAutomapper));
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddSingleton<JwtHelper>();

// ✅ JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// ✅ Swagger for testing APIs
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("3.0.1", new OpenApiInfo
    {
        Version = "3.0.1",
        Title = "Shopify API",
        Description = "An ASP.NET Core Web API for managing Shopify products"
    });

    // JWT support in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.  
Enter your token like this: **Bearer eyJhbGciOiJIUzI1NiIsInR5...**",
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
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// ----------------------------
// Middleware Pipeline
// ----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/3.0.1/swagger.json", "Shopify API 3.0.1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// ✅ Add CORS before authentication
app.UseCors("AllowFrontend");

app.UseAuthentication(); // Authentication must come before Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();
