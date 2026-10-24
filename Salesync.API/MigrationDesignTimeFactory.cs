using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Salesync.Infrastructure.Data;

namespace Salesync.API;

/// Model-only tooling. Never starts Program/seeders or reads production configuration.
public sealed class MigrationDesignTimeFactory : IDesignTimeDbContextFactory<SalesyncDbContext>
{
    public SalesyncDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<SalesyncDbContext>().UseSqlServer(
            "Server=127.0.0.1,1;Database=SalesyncModelOnly;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=1").Options);
}
