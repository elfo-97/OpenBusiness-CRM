using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Api.Security;
using OpenBusiness.Crm.Api.Services.Auth;
using OpenBusiness.Crm.Api.Services.Customers;
using OpenBusiness.Crm.Api.Services.Leads;
using OpenBusiness.Crm.Api.Services.Activities;
using OpenBusiness.Crm.Api.Services.Contacts;
using OpenBusiness.Crm.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() 
    ?? throw new InvalidOperationException("Jwt configuration missing");

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Owner", policy => policy.RequireRole("Owner"));
    options.AddPolicy("AdminOrOwner", policy => policy.RequireRole("Admin", "Owner"));
    options.AddPolicy("MemberOrAbove", policy => policy.RequireRole("Member", "Admin", "Owner"));
});

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IContactService, ContactService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
    .AllowAnonymous();

app.Run();
