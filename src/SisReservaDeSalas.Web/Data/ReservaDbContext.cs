using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SisReservaDeSalas.Web.Models;

namespace SisReservaDeSalas.Web.Data;

public class ReservaDbContext(DbContextOptions<ReservaDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Sala> Salas => Set<Sala>();
    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("btree_gist");

        var sala = modelBuilder.Entity<Sala>();
        sala.ToTable("Salas", tabela =>
            tabela.HasCheckConstraint("CK_Salas_Capacidade", "\"Capacidade\" > 0"));
        sala.HasKey(s => s.Id);
        sala.Property(s => s.Identificacao).HasMaxLength(100).IsRequired();
        sala.Property(s => s.Localizacao).HasMaxLength(150).IsRequired();
        sala.Property(s => s.Recursos).HasMaxLength(500);

        var reserva = modelBuilder.Entity<Reserva>();
        reserva.ToTable("Reservas", tabela =>
            tabela.HasCheckConstraint("CK_Reservas_Periodo", "\"Fim\" > \"Inicio\""));
        reserva.Property(r => r.Inicio).HasColumnType("timestamp without time zone");
        reserva.Property(r => r.Fim).HasColumnType("timestamp without time zone");
        reserva.Property(r => r.Finalidade).HasMaxLength(200).IsRequired();
        reserva.HasOne(r => r.Sala).WithMany().HasForeignKey(r => r.SalaId).OnDelete(DeleteBehavior.Restrict);
        reserva.HasOne(r => r.Usuario).WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        reserva.HasIndex(r => new { r.SalaId, r.Inicio, r.Fim });
    }
}
