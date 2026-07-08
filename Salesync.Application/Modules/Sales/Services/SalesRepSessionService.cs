using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;
using System;

namespace Salesync.Application.Modules.Sales.Services
{
    public class SalesRepSessionService : ISalesRepSessionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;


        public SalesRepSessionService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<SalesRepSessionDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid session id.");

            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (session == null)
                throw new KeyNotFoundException($"SalesRepSession with id {id} not found.");

            await EnsureSalesRepCanAccessSessionAsync(session.SalesRepId);

            return _mapper.Map<SalesRepSessionDto>(session);
        }

        public async Task<IEnumerable<SalesRepSessionDto>> GetBySalesRepAsync(int salesRepId)
        {
            if (salesRepId <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            await EnsureSalesRepCanAccessSessionAsync(salesRepId);

            var sessions = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.SalesRepId == salesRepId && x.IsActive)
                .OrderByDescending(x => x.WorkingDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepSessionDto>>(sessions);
        }

        public async Task<IEnumerable<SalesRepSessionDto>> GetActiveSessionsAsync()
        {
            var query = _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.Status != DayStatus.Closed && x.IsActive);

            query = await ApplySalesRepAccessFilterAsync(query);

            var sessions = await query
                .OrderByDescending(x => x.WorkingDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepSessionDto>>(sessions);
        }

        public async Task<IEnumerable<SalesRepSessionDto>> GetClosedSessionsAsync()
        {
            var query = _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.Status == DayStatus.Closed && x.IsActive);

            query = await ApplySalesRepAccessFilterAsync(query);

            var sessions = await query
                .OrderByDescending(x => x.WorkingDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepSessionDto>>(sessions);
        }

        public async Task<SalesRepSessionDto> StartSessionAsync(CreateSalesRepSessionDto dto)
        {
            var salesRep = await GetSessionSalesRepAsync(dto.SalesRepId);

            var today = DateTime.UtcNow.Date;

            var hasOpenSessionToday = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AnyAsync(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.WorkingDate == today &&
                    x.Status != DayStatus.Closed &&
                    x.IsActive);

            if (hasOpenSessionToday)
                throw new InvalidOperationException("You already have an open session today.");

            var session = new SalesRepSession
            {
                SalesRepId = salesRep.Id,
                WorkingDate = today,
                Status = DayStatus.Started,
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.SalesRepSessions.AddAsync(session);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<SalesRepSessionDto>(session);
        }

        public async Task<SalesRepSessionDto> CloseSessionAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid session id.");

            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (session == null)
                throw new KeyNotFoundException($"Session with id {id} not found.");

            await EnsureSalesRepCanAccessSessionAsync(session.SalesRepId);

            if (session.Status == DayStatus.Closed)
                throw new InvalidOperationException($"Session with id {id} is already closed.");

            var invoices = await _unitOfWork.Invoices
                .GetQueryable()
                .Where(x =>
                    x.SalesRepSessionId == id &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.IsActive)
                .ToListAsync();

            var payments = await _unitOfWork.Payments
                .GetQueryable()
                .Where(x =>
                    x.SalesRepSessionId == id &&
                    x.Status == PaymentStatus.Paid &&
                    x.IsActive)
                .ToListAsync();

            var returns = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .Where(x =>
                    x.SalesRepSessionId == id &&
                    x.Status == ReturnStatus.Approved &&
                    x.IsActive)
                .ToListAsync();

            var totalVisits = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .CountAsync(x =>
                    x.SalesRepSessionId == id &&
                    x.Status == VisitStatus.Completed &&
                    x.IsActive);

            var grossSales = invoices.Sum(x => x.SubTotal);
            var totalInvoices = invoices.Count;
            var totalCollection = payments.Sum(x => x.Amount);
            var totalReturnAmount = returns.Sum(x => x.TotalAmount);
            var netSales = invoices.Sum(x => x.TotalAmount) - totalReturnAmount;

            session.GrossSales = grossSales;
            session.NetSales = netSales;
            session.TotalCollection = totalCollection;
            session.TotalReturnAmount = totalReturnAmount;
            session.TotalInvoices = totalInvoices;
            session.TotalVisits = totalVisits;

            session.Status = DayStatus.Closed;
            session.EndTime = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.SalesRepSessions.Update(session);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<SalesRepSessionDto>(session);
        }



        #region Helper Methods

        private async Task<SalesRepEntity> GetSessionSalesRepAsync(int? dtoSalesRepId)
        {
            if (_currentUser.Role == "SalesRep")
            {
                var currentSalesRep = await _unitOfWork.SalesReps
                    .GetQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.UserId == _currentUser.UserId &&
                        x.IsActive);

                if (currentSalesRep == null)
                    throw new UnauthorizedAccessException("Current user is not linked to an active sales rep.");

                return currentSalesRep;
            }

            if (!dtoSalesRepId.HasValue || dtoSalesRepId.Value <= 0)
                throw new ArgumentException("SalesRepId is required.");

            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == dtoSalesRepId.Value &&
                    x.IsActive);

            if (salesRep == null)
                throw new KeyNotFoundException("Sales rep not found.");

            return salesRep;
        }

        private async Task EnsureSalesRepCanAccessSessionAsync(int salesRepId)
        {
            if (_currentUser.Role != "SalesRep")
                return;

            var currentSalesRepId = await _unitOfWork.SalesReps
                .GetQueryable()
                .Where(x =>
                    x.UserId == _currentUser.UserId &&
                    x.IsActive)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (currentSalesRepId == 0 || currentSalesRepId != salesRepId)
                throw new UnauthorizedAccessException("You are not allowed to access this session.");
        }

        private async Task<IQueryable<SalesRepSession>> ApplySalesRepAccessFilterAsync(IQueryable<SalesRepSession> query)
        {
            if (_currentUser.Role != "SalesRep")
                return query;

            var currentSalesRepId = await _unitOfWork.SalesReps
                .GetQueryable()
                .Where(x =>
                    x.UserId == _currentUser.UserId &&
                    x.IsActive)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (currentSalesRepId == 0)
                throw new UnauthorizedAccessException("Current user is not linked to an active sales rep.");

            return query.Where(x => x.SalesRepId == currentSalesRepId);
        } 

        #endregion
    }
}