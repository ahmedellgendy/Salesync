using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salesync.Application;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Mappings;
using Salesync.Application.Services;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Salesync.Infrastructure.Data;
using Salesync.Infrastructure.Repositories.Common;

namespace Salesync.Tests;

public sealed class Actor : ICurrentUserService
{
    public string? UserId { get; set; } = "rep-1";
    public string? Role { get; set; } = "SalesRep";
    public int? SalesRepId => 1;
    public int? BranchId { get; set; } = 1;
    public int? BusinessUnitId => null;
}

// Isolated in-memory database: no configuration files, host startup or SQL Server connection.
public sealed class TestDb(DbContextOptions<SalesyncDbContext> options) : SalesyncDbContext(options)
{
    public bool FailClosingInsert { get; set; }
    public bool FailReturnStock { get; set; }
    public bool FailTreasuryAudit { get; set; }
    public bool FailVisitCompletion { get; set; }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (FailReturnStock && ChangeTracker.Entries<Salesync.Domain.Modules.Inventory.Entities.StockMovement>().Any(x => x.State == EntityState.Added))
            throw new InvalidOperationException("Injected return stock failure");
        if (FailClosingInsert && ChangeTracker.Entries<SalesRepDayClosing>().Any(x => x.State == EntityState.Added))
            throw new InvalidOperationException("Injected closing insert failure");
        if (FailTreasuryAudit && ChangeTracker.Entries<Salesync.Domain.Modules.Treasury.Entities.TreasuryTransaction>().Any(x => x.State == EntityState.Added))
            throw new InvalidOperationException("Injected treasury audit failure");
        if (FailVisitCompletion && ChangeTracker.Entries<Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit>().Any(x => x.State == EntityState.Modified))
            throw new InvalidOperationException("Injected visit linking failure");
        return base.SaveChangesAsync(cancellationToken);
    }
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        foreach (var entity in model.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            property.SetColumnType(null);
            if (property.GetDefaultValueSql() != null) property.SetDefaultValueSql(null);
        }
    }
}

public sealed class WorkflowFixture : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public TestDb Db { get; private set; } = null!;
    public UnitOfWork Unit { get; private set; } = null!;
    public Actor Actor { get; } = new();
    public ServiceProvider Services { get; private set; } = null!;
    public T Get<T>() where T : notnull => Services.GetRequiredService<T>();
    public static async Task<WorkflowFixture> CreateAsync()
    {
        var fixture = new WorkflowFixture();
        await fixture.connection.OpenAsync();
        fixture.Db = new TestDb(new DbContextOptionsBuilder<SalesyncDbContext>().UseSqlite(fixture.connection).Options);
        await fixture.Db.Database.EnsureCreatedAsync();
        fixture.Unit = new UnitOfWork(fixture.Db);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        services.AddAutoMapper(typeof(MappingProfile));
        services.AddSingleton<IUnitOfWork>(fixture.Unit);
        services.AddSingleton<ICurrentUserService>(fixture.Actor);
        services.AddSingleton<IBusinessClock>(new BusinessClock(TimeProvider.System, "Africa/Cairo"));
        fixture.Services = services.BuildServiceProvider();
        fixture.Db.AddRange(new Salesync.Infrastructure.Modules.Identity.Entities.ApplicationUser { Id="rep-1",FullName="Rep 1" },
            new Salesync.Infrastructure.Modules.Identity.Entities.ApplicationUser { Id="rep-2",FullName="Rep 2" },
            new Salesync.Infrastructure.Modules.Identity.Entities.ApplicationUser { Id="supervisor",FullName="Supervisor" });
        fixture.Db.AddRange(new Branch { Id=1, BranchCode="B1", Name="Branch 1" }, new Branch { Id=2, BranchCode="B2", Name="Branch 2" });
        fixture.Db.AddRange(new SalesRep { Id=10, SalesRepCode="S10", Name="Supervisor", Phone="10", BranchId=1, UserId="supervisor" },
            new SalesRep { Id=1, SalesRepCode="S1", Name="Rep 1", Phone="1", BranchId=1, UserId="rep-1", SupervisorId=10 },
            new SalesRep { Id=2, SalesRepCode="S2", Name="Rep 2", Phone="2", BranchId=2, UserId="rep-2" });
        fixture.Db.AddRange(new Warehouse { Id=1, WarehouseCode="W1", Name="Warehouse 1", BranchId=1 },
            new Warehouse { Id=2, WarehouseCode="W2", Name="Warehouse 2", BranchId=2 });
        fixture.Db.Add(new Customer { Id=1, Name="Customer" });
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();
        return fixture;
    }
    public async Task<SalesRepSession> SessionAsync(int id=1, int rep=1, bool settled=false)
    {
        var session = new SalesRepSession { Id=id, SalesRepId=rep, WorkingDate=DateTime.Today.AddDays(-id),
            StartTime=DateTime.UtcNow.AddHours(-1), Status=Salesync.Domain.Common.Enums.Sales.DayStatus.Started, IsStockSettled=settled };
        Db.Add(session); await Db.SaveChangesAsync(); return session;
    }
    public async ValueTask DisposeAsync() { await Services.DisposeAsync(); await Db.DisposeAsync(); await connection.DisposeAsync(); }
}
