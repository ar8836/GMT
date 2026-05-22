using Microsoft.EntityFrameworkCore;
using GMT.Models;

namespace GMT.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Login> Logins { get; set; }
        public DbSet<Alumno> Alumnos { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Convenio> Convenios { get; set; }
        public DbSet<SolicitudPractica> SolicitudesPracticas { get; set; }
        public DbSet<DocumentoAlumno> DocumentosAlumno { get; set; }
        public DbSet<RegistroPendiente> RegistrosPendientes { get; set; }
        public DbSet<PlazaPractica> PlazasPracticas { get; set; }
        public DbSet<DocumentoEmpresa> DocumentosEmpresa { get; set; }
        public DbSet<VerificacionEmpresa> VerificacionesEmpresa { get; set; }
        public DbSet<ListaNegraEmpresa> ListaNegraEmpresas { get; set; }
        // ── Fase 3 ────────────────────────────────────────────────────────────
        public DbSet<Entrevista> Entrevistas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Login>(e =>
            {
                e.ToTable("login");
                e.HasIndex(l => l.CorreoInstitucional).IsUnique();
            });

            modelBuilder.Entity<Alumno>(e =>
            {
                e.ToTable("alumnos");
                e.HasIndex(a => a.LoginId).IsUnique();
                e.HasIndex(a => a.NumeroControl).IsUnique();
                e.HasOne(a => a.Login).WithOne(l => l.Alumno)
                 .HasForeignKey<Alumno>(a => a.LoginId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Empresa>(e =>
            {
                e.ToTable("empresas");
                e.HasIndex(em => em.LoginId).IsUnique();
                e.HasIndex(em => em.RFC).IsUnique();
                e.HasOne(em => em.Login).WithOne(l => l.Empresa)
                 .HasForeignKey<Empresa>(em => em.LoginId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Convenio>(e =>
            {
                e.ToTable("convenios");
                e.HasOne(c => c.Empresa).WithMany(em => em.Convenios)
                 .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlazaPractica>(e =>
            {
                e.ToTable("plazas_practicas");
                e.HasOne(p => p.Empresa).WithMany(em => em.Plazas)
                 .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SolicitudPractica>(e =>
            {
                e.ToTable("solicitudes_practicas");
                e.HasOne(s => s.Alumno).WithMany(a => a.Solicitudes)
                 .HasForeignKey(s => s.AlumnoId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(s => s.Empresa).WithMany(em => em.Solicitudes)
                 .HasForeignKey(s => s.EmpresaId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(s => s.Convenio).WithMany(c => c.Solicitudes)
                 .HasForeignKey(s => s.ConvenioId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(s => s.Plaza).WithMany(p => p.Solicitudes)
                 .HasForeignKey(s => s.PlazaId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<DocumentoAlumno>(e =>
            {
                e.ToTable("DocumentosAlumno");
                e.HasOne(d => d.Alumno).WithMany(a => a.Documentos)
                 .HasForeignKey(d => d.AlumnoId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DocumentoEmpresa>(e =>
            {
                e.ToTable("documentos_empresa");
                e.HasOne(d => d.Empresa).WithMany(em => em.DocumentosEmpresa)
                 .HasForeignKey(d => d.EmpresaId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<VerificacionEmpresa>(e =>
            {
                e.ToTable("verificaciones_empresa");
                e.HasOne(v => v.Empresa).WithMany()
                 .HasForeignKey(v => v.EmpresaId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ListaNegraEmpresa>(e =>
            {
                e.ToTable("lista_negra_empresas");
                e.HasIndex(l => l.EmpresaId).IsUnique();
                e.HasOne(l => l.Empresa).WithMany()
                 .HasForeignKey(l => l.EmpresaId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Entrevista>(e =>
            {
                e.ToTable("entrevistas");
                e.HasOne(en => en.Solicitud).WithMany()
                 .HasForeignKey(en => en.SolicitudId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(en => en.Alumno).WithMany()
                 .HasForeignKey(en => en.AlumnoId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(en => en.Empresa).WithMany()
                 .HasForeignKey(en => en.EmpresaId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RegistroPendiente>(e =>
            {
                e.ToTable("registros_pendientes");
            });
        }
    }
}
