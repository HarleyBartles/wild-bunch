using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WildBunch.Persistence;

public sealed class WildBunchDbContextFactory : IDesignTimeDbContextFactory<WildBunchDbContext>
{
    public WildBunchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseSqlite("Data Source=wildbunch.db")
            .Options;

        return new WildBunchDbContext(options);
    }
}
