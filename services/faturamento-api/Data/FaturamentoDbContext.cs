using faturamento_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace faturamento_api.Data;

public class FaturamentoDbContext : DbContext
{
    public FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options) : base(options)
    {
    }

    public DbSet<Nota> Notas => Set<Nota>();
    public DbSet<ItemNota> ItensNota => Set<ItemNota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Nota>()
            .ToTable("Invoices")
            .HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemNota>().ToTable("InvoiceItems");

        base.OnModelCreating(modelBuilder);
    }
}
