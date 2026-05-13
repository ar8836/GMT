using GMT.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:5001")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// PostgreSQL en AWS RDS
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => { options.LoginPath = "/Account/Index"; });

// AWS SDK
var awsOptions = builder.Configuration.GetAWSOptions();
var accessKey = builder.Configuration["AWS:AccessKey"];
var secretKey = builder.Configuration["AWS:SecretKey"];

if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
    awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<Amazon.S3.IAmazonS3>();
builder.Services.AddAWSService<Amazon.SimpleEmail.IAmazonSimpleEmailService>();

// Servicios propios
builder.Services.AddScoped<GMT.Services.S3Service>();
builder.Services.AddScoped<GMT.Services.EmailService>();
builder.Services.AddHttpClient<GMT.Services.RfcValidationService>();
builder.Services.AddScoped<GMT.Services.IRfcValidationService, GMT.Services.RfcValidationService>();
builder.Services.AddScoped<GMT.Services.IVerificationService, GMT.Services.VerificationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.Run();
