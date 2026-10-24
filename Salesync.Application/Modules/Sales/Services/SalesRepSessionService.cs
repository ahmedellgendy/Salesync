using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Application.Interfaces.Services;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.UnloadRequest;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using System;

namespace Salesync.Application.Modules.Sales.Services
{
    public class SalesRepSessionService : ISalesRepSessionService
    {
        private readonly ISalesAccessScopeService _access;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ISalesRepDayClosingService _closings;
        private readonly IBusinessClock _clock;

        public SalesRepSessionService(IUnitOfWork unitOfWork, IMapper mapper,
            ISalesAccessScopeService access, ISalesRepDayClosingService closings, IBusinessClock clock)
        {
            _access = access;
            _closings = closings;
            _clock = clock;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<SalesRepSessionDto> GetByIdAsync(int id)
        {
            var accessScope = await _access.GetAsync();
            var session = await _unitOfWork.SalesRepSessions.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"SalesRepSession with id {id} not found.");
            await _access.RequireSalesRepAsync(session.SalesRepId);
            return _mapper.Map<SalesRepSessionDto>(session);
        }

        public async Task<IEnumerable<SalesRepSessionDto>> GetBySalesRepAsync(int SalesRepId)
        {
            await _access.RequireSalesRepAsync(SalesRepId);
            var accessScope = await _access.GetAsync();
            var sessions = await _unitOfWork.SalesRepSessions.FindAsync(s => s.SalesRepId == SalesRepId);
            return _mapper.Map<IEnumerable<SalesRepSessionDto>>(sessions);
        }

        public async Task<IEnumerable<SalesRepSessionDto>> GetActiveSessionsAsync()
        {
            var accessScope = await _access.GetAsync();
            var sessions = await _unitOfWork.SalesRepSessions
                .FindAsync(s => s.Status != DayStatus.Closed && (accessScope.AllowsAll || accessScope.SalesRepIds.Contains(s.SalesRepId)));
            return _mapper.Map<IEnumerable<SalesRepSessionDto>>(sessions);
        }

        public async Task<IEnumerable<SalesRepSessionDto>> GetClosedSessionsAsync()
        {
            var accessScope = await _access.GetAsync();
            var sessions = await _unitOfWork.SalesRepSessions
                .FindAsync(s => s.Status == DayStatus.Closed && (accessScope.AllowsAll || accessScope.SalesRepIds.Contains(s.SalesRepId)));
            return _mapper.Map<IEnumerable<SalesRepSessionDto>>(sessions);
        }

        public Task<SalesRepSessionDto> StartSessionAsync(CreateSalesRepSessionDto dto) =>
            _unitOfWork.ExecuteInTransactionAsync(() => StartSessionAsyncCore(dto));

        private async Task<SalesRepSessionDto> StartSessionAsyncCore(CreateSalesRepSessionDto dto)
        {
            var accessScope = await _access.GetAsync();
            await _access.RequireSalesRepAsync(dto.SalesRepId);
            // chech if there is already an open session for the sales rep on the same working date
            var hasSessionToday = await _unitOfWork.SalesRepSessions
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .AnyAsync(x => x.SalesRepId == dto.SalesRepId && x.WorkingDate == _clock.Today && x.IsActive);

            if (hasSessionToday)
                throw new InvalidOperationException("Sales rep already has a session today.");

            if (await _unitOfWork.SalesRepSessions.GetQueryable().AnyAsync(x => x.SalesRepId == dto.SalesRepId &&
                x.IsActive && x.Status != DayStatus.Closed && !x.EndTime.HasValue))
                throw new InvalidOperationException("OPEN_SESSION: توجد جلسة سابقة مفتوحة. افتح مراجعة نهاية اليوم وأكمل إقفالها قبل بدء يوم جديد.");

            var session = new SalesRepSession
            {
                SalesRepId = dto.SalesRepId,
                WorkingDate = _clock.Today,
                Status = DayStatus.Started,
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.SalesRepSessions.AddAsync(session);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<SalesRepSessionDto>(session);

        }
        public Task<SalesRepSessionDto> CloseSessionAsync(int id, int? warehouseId = null) =>
            _unitOfWork.ExecuteInTransactionAsync(() => CloseSessionAsyncCore(id, warehouseId));

        private async Task<SalesRepSessionDto> CloseSessionAsyncCore(int id, int? warehouseId)
        {
            var accessScope = await _access.GetAsync();
            if (id <= 0)
                throw new ArgumentException("Invalid session id.");

            var session = await _unitOfWork.SalesRepSessions.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Session with id {id} not found.");
            await _access.RequireSalesRepAsync(session.SalesRepId);

            var existingClosing = await _unitOfWork.SalesRepDayClosings.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(x => x.SalesRepSessionId == id && x.IsActive && x.Status != SalesRepDayClosingStatus.Cancelled);
            if (session.Status == DayStatus.Closed && existingClosing != null)
                return _mapper.Map<SalesRepSessionDto>(session);
            if (existingClosing != null) throw new InvalidOperationException("SETTLEMENT_INCONSISTENT: Session requires review.");
            if (await _unitOfWork.CustomerVisits.GetQueryable().AnyAsync(x => x.SalesRepSessionId == id &&
                x.IsActive && x.Status == VisitStatus.InProgress && !x.EndTime.HasValue))
                throw new InvalidOperationException("OPEN_VISITS: Complete the open visits before closing.");

            // 1. امنع قفل اليوم لو فيه طلب تفريغ لسه مستني المخزن
            var hasPendingUnloadRequest = await _unitOfWork.SalesRepUnloadRequests
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .AnyAsync(x =>
                    x.SalesRepSessionId == id &&
                    x.SalesRepId == session.SalesRepId &&
                    x.IsActive &&
                    (x.Status == UnloadRequestStatus.PendingWarehouse || x.Status == UnloadRequestStatus.Draft));

            if (hasPendingUnloadRequest)
                throw new InvalidOperationException("Cannot close day while unload request is still pending warehouse confirmation.");

            // 2. امنع قفل اليوم لو المندوب لسه معاه بضاعة
            var hasRemainingStock = await _unitOfWork.SalesRepInventories
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .AnyAsync(x =>
                    x.SalesRepId == session.SalesRepId &&
                    x.IsActive &&
                    x.Quantity != 0);

            if (hasRemainingStock)
                throw new InvalidOperationException("Cannot close day before unloading the remaining stock to warehouse.");

            var unloadWarehouses = await _unitOfWork.SalesRepUnloadRequests.GetQueryable().AsNoTracking()
                .Where(x => x.SalesRepSessionId == id && x.IsActive &&
                    (x.Status == UnloadRequestStatus.Confirmed || x.Status == UnloadRequestStatus.PartiallyConfirmed))
                .Select(x => x.WarehouseId).Distinct().ToListAsync();
            if (unloadWarehouses.Count > 1) throw new InvalidOperationException("MULTIPLE_WAREHOUSES: Review the stock settlement.");
            if (unloadWarehouses.Count == 1 && warehouseId.HasValue && warehouseId != unloadWarehouses[0])
                throw new InvalidOperationException("WAREHOUSE_MISMATCH: Use the actual unload warehouse.");
            var closingWarehouseId = unloadWarehouses.Count == 1 ? unloadWarehouses[0] : warehouseId;
            if (!closingWarehouseId.HasValue) throw new InvalidOperationException("WAREHOUSE_REQUIRED: Select an authorized settlement warehouse.");
            await _access.RequireWarehouseAsync(closingWarehouseId.Value);
            session.IsStockSettled = true;
            session.StockSettledAt ??= DateTime.UtcNow;

            var invoices = await _unitOfWork.Invoices
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId ?? 0))
                .Where(i =>
                    i.SalesRepSessionId == id &&
                    i.Status == InvoiceStatus.Confirmed &&
                    i.IsActive)
                .ToListAsync();

            var payments = await _unitOfWork.Payments
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId ?? 0))
                .Where(p =>
                    p.SalesRepSessionId == id &&
                    p.Status == PaymentStatus.Paid &&
                    p.IsActive)
                .ToListAsync();

            var returns = await _unitOfWork.InvoiceReturns
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId ?? 0))
                .Where(r =>
                    r.SalesRepSessionId == id &&
                    r.Status == ReturnStatus.Approved &&
                    r.IsActive)
                .ToListAsync();

            var grossSales = invoices.Sum(i => i.SubTotal);
            var totalInvoices = invoices.Count;
            var totalCollection = payments.Sum(p => p.Amount);
            var totalReturnAmount = returns.Sum(r => r.TotalAmount);
            var netSales = invoices.Sum(i => i.TotalAmount) - totalReturnAmount;

            var totalVisits = await _unitOfWork.CustomerVisits
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .CountAsync(x =>
                    x.SalesRepSessionId == id &&
                    x.Status == VisitStatus.Completed &&
                    x.IsActive);

            session.GrossSales = grossSales;
            session.NetSales = netSales;
            session.TotalCollection = totalCollection;
            session.TotalReturnAmount = totalReturnAmount;
            session.TotalInvoices = totalInvoices;
            session.TotalVisits = totalVisits;

            session.Status = DayStatus.Closed;
            session.EndTime ??= DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.IsActive = true;

            _unitOfWork.SalesRepSessions.Update(session);
            await _unitOfWork.CompleteAsync();

            await _closings.CreateAsync(new CreateSalesRepDayClosingDto {
                SalesRepSessionId = session.Id, WarehouseId = closingWarehouseId.Value,
                Notes = "Created by the atomic day closing workflow." });
            return _mapper.Map<SalesRepSessionDto>(session);
        }
    }
}
