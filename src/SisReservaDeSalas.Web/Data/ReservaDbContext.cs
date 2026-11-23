using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SisReservaDeSalas.Web.Models;

namespace SisReservaDeSalas.Web.Data;

public class ReservaDbContext(DbContextOptions<ReservaDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Sala> Salas => Set<Sala>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var sala = modelBuilder.Entity<Sala>();
        sala.ToTable("Salas", tabela =>
            tabela.HasCheckConstraint("CK_Salas_Capacidade", "\"Capacidade\" > 0"));
        sala.HasKey(s => s.Id);
        sala.Property(s => s.Identificacao).HasMaxLength(100).IsRequired();
        sala.Property(s => s.Localizacao).HasMaxLength(150).IsRequired();
        sala.Property(s => s.Recursos).HasMaxLength(500);
    }
}
