using BLL.Interfaces;
using BLL.Repositories;
using DAL.DbContext;
using DAL.DTOs.MappingProfile;
using DAL.Entities.AppUser;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Restaurant.AuthorizationPipline;
using Restaurant.Extensions;
using Restaurant.SeedingData;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<RestaurantDB>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RESDB")));
builder.Services.AddRateLimiter(Limit =>
{
Limit.AddFixedWindowLimiter("Fixed", option =>
    {
        option.PermitLimit = 5;
        option.Window = TimeSpan.FromMinutes(15);
        option.QueueLimit = 0;
    });
Limit.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("كثير من الطلبات حاول مجددا فى وقت لاحق", token);
    };
});
builder.Services.AddAuthorization(option =>
{
    option.AddPolicy("ValidToken", policy =>
    {
        policy.Requirements.Add(new AuthorizationRequirment());
    });
});
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<RestaurantDB>()
    .AddSignInManager<SignInManager<ApplicationUser>>()
    .AddDefaultTokenProviders();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<RestaurantDB>();
builder.Services.AddCustomJwtAuth(builder.Configuration);
builder.Services.AddSwaggerGenAuth();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthorizationHandler,Restaurant.AuthorizationPipline.TokenHandler>();
builder.Services.AddAutoMapper(typeof(MapProfile).Assembly);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("https://restaurantapp-bice.vercel.app")
       .AllowAnyMethod()
       .AllowAnyHeader();
    });
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider;
    await Seeding.DataSeeding(service);
}
app.Use(async (context, next) =>
{
    await next();

    if (!context.Response.HasStarted)
    {
        if (context.Response.StatusCode == 401)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\": \"يجب تسجيل الدخول أولا\"}");
        }

        if (context.Response.StatusCode == 403)
        {
            context.Response.ContentType = "application/json";
            var errorType = context.Response.Headers["auth"].ToString();

            if (errorType == "InvalidToken")
            {
                await context.Response.WriteAsync("{\"error\": \"انتهت الجلسة أعد تسجيل الدخول مجددا\"}");
            }
            else if (errorType == "UserNotFound")
            {
                await context.Response.WriteAsync("{\"error\": \"هذا المستخدم غير موجود\"}");
            }
            else
            {
                await context.Response.WriteAsync("{\"error\": \"غير مسموح لك\"}");
            }
        }
    }
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
