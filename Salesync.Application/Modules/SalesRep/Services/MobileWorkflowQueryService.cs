using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Domain.Common.Enums.UnloadRequest;
using Salesync.Domain.Common.Enums.LoadRequest;

namespace Salesync.Application.Modules.SalesRep.Services;

/// Read models use database identity/scope. No client-side reconstruction of ledgers.
public sealed class MobileWorkflowQueryService(IUnitOfWork unit, ISalesAccessScopeService access,
    IBusinessClock clock, IMapper mapper, ISalesRepDayClosingCalculator calculator, ISalesRepDayClosingService closings)
{
    private async Task<int> OwnRepAsync()
    {
        var scope = await access.GetAsync();
        if (scope.AllowsAll || scope.SalesRepIds.Length != 1)
            throw new UnauthorizedAccessException("A current representative profile is required.");
        return scope.SalesRepIds.Single();
    }

    public async Task<MobilePage<SalesRepSessionDto>> SessionsAsync(DateTime? from, DateTime? to, int page, int size)
    {
        Validate(page, size, from, to);
        var rep = await OwnRepAsync();
        var query = unit.SalesRepSessions.GetQueryable().AsNoTracking().Where(x => x.IsActive && x.SalesRepId == rep);
        if (from.HasValue) query = query.Where(x => x.WorkingDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(x => x.WorkingDate < to.Value.Date.AddDays(1));
        var count = await query.CountAsync();
        var records = await query.OrderByDescending(x => x.WorkingDate).ThenByDescending(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync();
        return new(mapper.Map<List<SalesRepSessionDto>>(records), count, page, size, Pages(count, size));
    }

    public async Task<MobilePaymentPage> PaymentsAsync(int? sessionId, DateTime? from, DateTime? to, int page, int size)
    {
        Validate(page, size, from, to);
        var rep = await OwnRepAsync();
        if (sessionId.HasValue && !await unit.SalesRepSessions.GetQueryable().AnyAsync(s => s.Id == sessionId && s.SalesRepId == rep && s.IsActive))
            throw new KeyNotFoundException("Session not found in your scope.");
        var query = unit.Payments.GetQueryable().AsNoTracking().Where(x => x.IsActive && x.SalesRepId == rep && x.Status == PaymentStatus.Paid);
        if (sessionId.HasValue) query = query.Where(x => x.SalesRepSessionId == sessionId);
        if (from.HasValue) { var start = clock.UtcStart(from.Value); query = query.Where(x => x.PaymentDate >= start); }
        if (to.HasValue) { var end = clock.UtcStart(to.Value.Date.AddDays(1)); query = query.Where(x => x.PaymentDate < end); }
        var totalCount = await query.CountAsync();
        var totalAmount = await query.SumAsync(x => (decimal?)x.Amount) ?? 0;
        var cashAmount = await query.Where(x => x.PaymentMethod == PaymentMethod.Cash).SumAsync(x => (decimal?)x.Amount) ?? 0;
        var rows = await query.Include(x => x.Invoice).Include(x => x.Customer).Include(x => x.SalesRep)
            .OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync();
        var items = mapper.Map<List<PaymentDto>>(rows);
        for (var i = 0; i < rows.Count; i++) {
            items[i].InvoiceNumber = rows[i].Invoice.InvoiceNumber;
            items[i].CustomerName = rows[i].Customer.Name;
        }
        return new(items, totalCount, page, size, Pages(totalCount, size), new(totalAmount, cashAmount, totalAmount - cashAmount));
    }

    public async Task<MobileReviewDto> ReviewAsync(int sessionId, int? warehouseId)
    {
        var rep = await OwnRepAsync();
        var session = await unit.SalesRepSessions.GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.SalesRepId == rep && x.IsActive)
            ?? throw new KeyNotFoundException("Session not found in your scope.");
        var open = await unit.CustomerVisits.GetQueryable().AnyAsync(x => x.SalesRepSessionId == sessionId &&
            x.IsActive && x.Status == VisitStatus.InProgress && !x.EndTime.HasValue);
        var remaining = await unit.SalesRepInventories.GetQueryable().AnyAsync(x => x.SalesRepId == rep && x.IsActive && x.Quantity != 0);
        var unloads = unit.SalesRepUnloadRequests.GetQueryable().AsNoTracking().Where(x => x.SalesRepSessionId == sessionId && x.IsActive);
        var pending = await unloads.AnyAsync(x => x.Status == UnloadRequestStatus.PendingWarehouse || x.Status == UnloadRequestStatus.Draft);
        var pendingReturns = await unit.InvoiceReturns.GetQueryable().AnyAsync(x =>
            x.SalesRepSessionId == sessionId && x.IsActive && x.Status == ReturnStatus.Pending);
        var warehouses = await unloads.Where(x => x.Status == UnloadRequestStatus.Confirmed || x.Status == UnloadRequestStatus.PartiallyConfirmed)
            .Select(x => x.WarehouseId).Distinct().ToListAsync();
        var scope = await access.GetAsync();
        var options = await unit.Warehouses.GetQueryable().AsNoTracking().Where(x => x.IsActive && scope.BranchIds.Contains(x.BranchId))
            .OrderBy(x => x.Name).Select(x => new MobileWarehouseOptionDto { Id = x.Id, Name = x.Name,
                WarehouseCode = x.WarehouseCode, BranchId = x.BranchId, Location = x.Location }).ToListAsync();
        var selected = warehouses.Count == 1 ? warehouses[0] : warehouseId;
        var warehouseValid = warehouses.Count <= 1 && selected.HasValue && options.Any(x => x.Id == selected.Value) &&
            (!warehouseId.HasValue || warehouses.Count == 0 || warehouseId.Value == warehouses[0]);
        var records = (await closings.GetAllAsync(new SalesRepDayClosingFilterDto { SalesRepSessionId = sessionId }))
            .Where(c => c.Status != SalesRepDayClosingStatus.Cancelled).ToList();
        var checks = new List<MobileReviewCheck> {
            new("OPEN_VISITS", "إنهاء الزيارات المفتوحة", !open),
            new("STOCK_REMAINING", "تسوية البضاعة المتبقية", !remaining),
            new("PENDING_UNLOAD", "تأكيد المخزن لطلبات التفريغ", !pending),
            new("PENDING_RETURNS", "اعتماد أو رفض المرتجعات المعلقة من الإدارة", !pendingReturns),
            new("WAREHOUSE_REQUIRED", "اختيار مخزن التسوية المصرح به", warehouseValid),
            new("SETTLEMENT_INCONSISTENT", "اتساق مستند التسوية", records.Count <= 1),
        };
        return new() { Session = mapper.Map<SalesRepSessionDto>(session), Checks = checks,
            CanClose = checks.All(c => c.Passed) && session.Status != DayStatus.Closed && !session.IsTreasurySettled,
            WarehouseId = selected, Warehouses = options, Settlement = records.Count == 1 ? records.Single() : null,
            FinancialSummary = await calculator.CalculateAsync(new() { SalesRepSessionId = sessionId, WarehouseId = selected ?? 0 }, session) };
    }

    public async Task<MobileAttentionPage> AttentionAsync(DateTime? day, string? kind, int page, int size)
    {
        Validate(page, size, null, null);
        if (kind != null && !new[] { "load", "return", "closing", "visit" }.Contains(kind)) throw new ArgumentException("Invalid attention kind.");
        var scope = await access.GetAsync();
        var date = day?.Date ?? clock.Today;
        var from = clock.UtcStart(date); var to = clock.UtcStart(date.AddDays(1));
        var loads = unit.LoadRequests.GetQueryable().AsNoTracking().Where(x => x.IsActive &&
            scope.SalesRepIds.Contains(x.SalesRepId) && x.Status == LoadRequestStatus.Pending && x.RequestDate < to)
            .Select(x => new MobileAttentionItemDto { Kind = "load", DocumentId = x.Id, Reference = x.LoadRequestNumber,
                SalesRepId = x.SalesRepId, SalesRepName = x.SalesRep.Name, CustomerName = null, CreatedAt = x.RequestDate, Status = (int)x.Status, ExpectedCashAmount = null, CashVariance = null, StockVariance = null });
        var returns = unit.InvoiceReturns.GetQueryable().AsNoTracking().Where(x => x.IsActive &&
            scope.SalesRepIds.Contains(x.SalesRepId ?? 0) && x.Status == ReturnStatus.Pending && x.CreatedAt < to)
            .Select(x => new MobileAttentionItemDto { Kind = "return", DocumentId = x.Id, Reference = x.ReturnNumber,
                SalesRepId = x.SalesRepId ?? 0, SalesRepName = x.SalesRep!.Name, CustomerName = x.Customer.Name, CreatedAt = x.CreatedAt, Status = (int)x.Status, ExpectedCashAmount = null, CashVariance = null, StockVariance = null });
        var settlements = unit.SalesRepDayClosings.GetQueryable().AsNoTracking().Where(x => x.IsActive &&
            scope.SalesRepIds.Contains(x.SalesRepId) && x.ClosingDate < to && (x.Status == SalesRepDayClosingStatus.Submitted ||
            (x.Status == SalesRepDayClosingStatus.Completed && ((x.IsCashReceived && x.CashVariance != 0) || (x.IsStockReceived && x.TotalVarianceQuantity != 0)))))
            .Select(x => new MobileAttentionItemDto { Kind = "closing", DocumentId = x.Id, Reference = x.ClosingNumber,
                SalesRepId = x.SalesRepId, SalesRepName = x.SalesRep.Name, CustomerName = null, CreatedAt = x.ClosingDate, Status = (int)x.Status,
                ExpectedCashAmount = x.ExpectedCashAmount, CashVariance = x.IsCashReceived ? x.CashVariance : null,
                StockVariance = x.IsStockReceived ? x.TotalVarianceQuantity : null });
        var visits = unit.CustomerVisits.GetQueryable().AsNoTracking().Where(x => x.IsActive &&
            scope.SalesRepIds.Contains(x.SalesRepId) && x.VisitDate >= from && x.VisitDate < to && x.Status == VisitStatus.InProgress && !x.EndTime.HasValue)
            .Select(x => new MobileAttentionItemDto { Kind = "visit", DocumentId = x.Id, Reference = "VIS-" + x.Id,
                SalesRepId = x.SalesRepId, SalesRepName = x.SalesRep.Name, CustomerName = x.Customer.Name, CreatedAt = x.VisitDate, Status = (int)x.Status, ExpectedCashAmount = null, CashVariance = null, StockVariance = null });
        var all = loads.Concat(returns).Concat(settlements).Concat(visits);
        var counts = await all.GroupBy(x => x.Kind).Select(g => new { Kind = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Kind, x => x.Count);
        var selected = kind == null ? all : all.Where(x => x.Kind == kind);
        var count = await selected.CountAsync();
        var items = await selected.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Kind).ThenByDescending(x => x.DocumentId)
            .Skip((page - 1) * size).Take(size).ToListAsync();
        foreach (var category in new[] { "load", "return", "closing", "visit" }) counts.TryAdd(category, 0);
        return new(items, count, page, size, Pages(count, size), counts, date, DateTime.UtcNow);
    }

    private static int Pages(int count, int size) => (int)Math.Ceiling((double)count / size);
    private static void Validate(int page, int size, DateTime? from, DateTime? to)
    {
        if (page < 1 || size < 1 || size > 100 || page > int.MaxValue / size) throw new ArgumentException("Invalid pagination.");
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date) throw new ArgumentException("Invalid date range.");
    }
}
