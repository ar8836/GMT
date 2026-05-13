using Microsoft.EntityFrameworkCore;
using GMT.Models;

namespace GMT.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // ── DbSets ────────────────────────────────────────────────────────────
        public DbSet<Login> Logins { get; set; }
        public DbSet<Alumno> Alumnos { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Convenio> Convenios { get; set; }
        public DbSet<SolicitudPractica> SolicitudesPracticas { get; set; }
        public DbSet<DocumentoAlumno> DocumentosAlumno { get; set; }
        public DbSet<RegistroPendiente> RegistrosPendientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── login ────────────────────────────────────────────────────────
            modelBuilder.Entity<Login>(e =>
            {
                e.ToTable("login");
                e.HasIndex(l => l.CorreoInstitucional).IsUnique();
            });

            // ── alumnos ──────────────────────────────────────────────────────
            modelBuilder.Entity<Alumno>(e =>
            {
                e.ToTable("alumnos");
                e.HasIndex(a => a.LoginId).IsUnique();
                e.HasIndex(a => a.NumeroControl).IsUnique();

                e.HasOne(a => a.Login)
                 .WithOne(l => l.Alumno)
                 .HasForeignKey<Alumno>(a => a.LoginId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── empresas ─────────────────────────────────────────────────────
            modelBuilder.Entity<Empresa>(e =>
            {
                e.ToTable("empresas");
                e.HasIndex(em => em.LoginId).IsUnique();
                e.HasIndex(em => em.RFC).IsUnique();

                e.HasOne(em => em.Login)
                 .WithOne(l => l.Empresa)
                 .HasForeignKey<Empresa>(em => em.LoginId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── convenios ────────────────────────────────────────────────────
            modelBuilder.Entity<Convenio>(e =>
            {
                e.ToTable("convenios");

                e.HasOne(c => c.Empresa)
                 .WithMany(em => em.Convenios)
                 .HasForeignKey(c => c.EmpresaId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── solicitudes_practicas ────────────────────────────────────────
            modelBuilder.Entity<SolicitudPractica>(e =>
            {
                e.ToTable("solicitudes_practicas");

                e.HasOne(s => s.Alumno)
                 .WithMany(a => a.Solicitudes)
                 .HasForeignKey(s => s.AlumnoId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(s => s.Empresa)
                 .WithMany(em => em.Solicitudes)
                 .HasForeignKey(s => s.EmpresaId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(s => s.Convenio)
                 .WithMany(c => c.Solicitudes)
                 .HasForeignKey(s => s.ConvenioId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── DocumentosAlumno ─────────────────────────────────────────────
            modelBuilder.Entity<DocumentoAlumno>(e =>
            {
                e.ToTable("DocumentosAlumno");

                e.HasOne(d => d.Alumno)
                 .WithMany(a => a.Documentos)
                 .HasForeignKey(d => d.AlumnoId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── registros_pendientes ─────────────────────────────────────────
            modelBuilder.Entity<RegistroPendiente>(e =>
            {
                e.ToTable("registros_pendientes");
            });
        }
    }
}
