using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Configurar CORS para permitir solicitudes desde el frontend y otros servicios
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:5001") // Ajustar según el entorno
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Configurar la conexión a PostgreSQL en AWS
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register AWS SDK services with lazy credential validation
var awsOptions = builder.Configuration.GetAWSOptions();

// Extraer explícitamente las llaves del appsettings.json
var accessKey = builder.Configuration["AWS:AccessKey"];
var secretKey = builder.Configuration["AWS:SecretKey"];

// Si las llaves existen, inyectarlas en las opciones de AWS
if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
{
    awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
}
// Lazy loading: defer credential validation until first use (AWS SDK will validate on first request)
// No explicit ValidateCredentials property available in AWSOptions

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<Amazon.S3.IAmazonS3>();
builder.Services.AddAWSService<Amazon.SimpleEmail.IAmazonSimpleEmailService>();
builder.Services.AddScoped<GMT.Services.S3Service>();
builder.Services.AddScoped<GMT.Services.EmailService>();
builder.Services.AddScoped<GMT.Services.IRfcValidationService, GMT.Services.RfcValidationService>();

// Register RFC Validation Service
builder.Services.AddHttpClient<GMT.Services.RfcValidationService>(); // For IHttpClientFactory
builder.Services.AddScoped<GMT.Services.IRfcValidationService, GMT.Services.RfcValidationService>();

// builder.Services.AddRazorPages();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowSpecificOrigins"); // Habilitar CORS
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Cambiado de Home a Account

app.Run();
