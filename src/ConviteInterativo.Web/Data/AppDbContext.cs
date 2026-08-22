using ConviteInterativo.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Convite> Convites => Set<Convite>();
    public DbSet<Convidado> Convidados => Set<Convidado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Evento>(builder =>
        {
            builder.HasIndex(e => e.Slug).IsUnique();

            builder.Property(e => e.Anfitrioes).HasMaxLength(200);
            builder.Property(e => e.Homenageado).HasMaxLength(100);

            builder.HasMany(e => e.Convites)
                .WithOne(c => c.Evento)
                .HasForeignKey(c => c.EventoId);
        });

        modelBuilder.Entity<Convite>(builder =>
        {
            builder.HasIndex(c => c.Token).IsUnique();

            // HasDefaultValue exige o tipo CLR da propriedade (o enum), não a string
            // convertida — EF Core aplica o HasConversion ao valor default sozinho na
            // hora de gerar o SQL/migration (verificado: sai como defaultValue: "Grupo").
            builder.Property(c => c.ModoConfirmacao)
                .HasConversion<string>()
                .HasDefaultValue(ModoConfirmacao.Grupo);

            builder.HasMany(c => c.Convidados)
                .WithOne(g => g.Convite)
                .HasForeignKey(g => g.ConviteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Convidado>(builder =>
        {
            builder.Property(g => g.Status)
                .HasConversion<string>();
        });
    }
}
