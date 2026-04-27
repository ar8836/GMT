using GMT.Models;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Login> Logins { get; set; }
    public DbSet<Alumno> Alumnos { get; set; }
    public DbSet<Empresa> Empresas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Login>().ToTable("login");
        modelBuilder.Entity<Alumno>().ToTable("alumnos");
        modelBuilder.Entity<Empresa>().ToTable("empresas");
    }
}
