using estoque_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace estoque_api.Data;

public class EstoqueDbContext : DbContext
{
    public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options)
    {
    }

    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>().ToTable("Products");

        base.OnModelCreating(modelBuilder);
    }
}
