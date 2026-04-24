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

// Configurar la conexi�n a PostgreSQL en AWS
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
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
