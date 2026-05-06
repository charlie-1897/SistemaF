using Microsoft.EntityFrameworkCore;
using SistemaF.Domain.Common;
using SistemaF.Domain.Entities.Anagrafica;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Entities.Prodotto;

namespace SistemaF.Infrastructure.Persistence;

public sealed class SistemaFDbContext(DbContextOptions<SistemaFDbContext> options)
    : DbContext(options)
{
    public DbSet<Prodotto>         Prodotti       => Set<Prodotto>();
    public DbSet<ScadenzaProdotto> Scadenze       => Set<ScadenzaProdotto>();
    public DbSet<Ordine>           Ordini         => Set<Ordine>();
    public DbSet<RigaOrdine>       RigheOrdine    => Set<RigaOrdine>();
    public DbSet<PropostaOrdine>   ProposteOrdine => Set<PropostaOrdine>();
    public DbSet<PropostaRiga>     ProposteRighe  => Set<PropostaRiga>();
    // Modulo Anagrafica
    public DbSet<Fornitore>                          Fornitori                        => Set<Fornitore>();
    public DbSet<Operatore>                          Operatori                        => Set<Operatore>();
    public DbSet<Farmacia>                           Farmacie                         => Set<Farmacia>();
    public DbSet<ConfigurazioneEmissione>            ConfigurazioniEmissione          => Set<ConfigurazioneEmissione>();
    public DbSet<ConfigurazioneEmissioneFornitore>   ConfigurazioniEmissioneFornitori => Set<ConfigurazioneEmissioneFornitore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SistemaFDbContext).Assembly);
        modelBuilder.Entity<Prodotto>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Ordine>().HasQueryFilter(o => !o.IsDeleted);
        modelBuilder.HasDefaultSchema("dbo");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ImpostaAuditTrail();
        return await base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        ImpostaAuditTrail();
        return base.SaveChanges();
    }

    private void ImpostaAuditTrail()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(Entity.CreatedAt)).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = now;
                    entry.Property(nameof(Entity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(Entity.CreatedBy)).IsModified = false;
                    break;
            }
        }
    }

    internal IReadOnlyList<IDomainEvent> GetDomainEvents()
        => ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.HasDomainEvents)
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

    internal void ClearAllDomainEvents()
    {
        foreach (var a in ChangeTracker.Entries<AggregateRoot>()
                     .Where(e => e.Entity.HasDomainEvents))
            a.Entity.ClearDomainEvents();
    }
}
