using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using System;

namespace Salesync.Application.Modules.Sales.Services
{
    public class SalesRepSessionService : ISalesRepSessionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SalesRepSessionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<SalesRepSessionDto> GetByIdAsync(int id)
        {
            var session = await _unitOfWork.SalesRepSessions.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"SalesRepSession with id {id} not found.");
            return _mapper.Map<SalesRepSessionDto>(session);
        }

        public async Task<IEnumerable<SalesRepSessionDto>> GetBySalesRepAsync(int SalesRepId)
        {
            var sessions = await _unitOfWork.SalesRepSessions.FindAsync(s => s.SalesRepId == SalesRepId);
            return _mapper.Map<IEnumerable<SalesRepSessionDto>>(sessions);
        }

        public async Task<IEnumerable<SalesRepSessionDto>> GetActiveSessionsAsync()
        {
            var sessions = await _unitOfWork.SalesRepSessions
                .FindAsync(s => s.Status != DayStatus.Closed);
            return _mapper.Map<IEnumerable<SalesRepSessionDto>>(sessions);
        }

        public async Task<IEnumerable<SalesRepSessionDto>> GetClosedSessionsAsync()
        {
            var sessions = await _unitOfWork.SalesRepSessions
                .FindAsync(s => s.Status == DayStatus.Closed);
            return _mapper.Map<IEnumerable<SalesRepSessionDto>>(sessions);
        }

        public async Task<SalesRepSessionDto> StartSessionAsync(CreateSalesRepSessionDto dto)
        {
            // chech if there is already an open session for the sales rep on the same working date
            var hasSessionToday = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AnyAsync(x => x.SalesRepId == dto.SalesRepId && x.WorkingDate == DateTime.UtcNow.Date && x.IsActive);

            if (hasSessionToday)
                throw new InvalidOperationException("Sales rep already has a session today.");

            var session = new SalesRepSession
            {
                SalesRepId = dto.SalesRepId,
                WorkingDate = DateTime.UtcNow.Date,
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
            var session = await _unitOfWork.SalesRepSessions.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Session with id {id} not found.");

            if (session.Status == DayStatus.Closed)
                throw new InvalidOperationException($"Session with id {id} is already closed.");

            var invoices = await _unitOfWork.Invoices
                .GetQueryable()
                .Where(i => i.SalesRepSessionId == id && i.Status == InvoiceStatus.Confirmed && i.IsActive)
                .ToListAsync();

            var payments = await _unitOfWork.Payments
                .GetQueryable()
                .Where(p => p.SalesRepSessionId == id && p.Status == PaymentStatus.Paid && p.IsActive)
                .ToListAsync();

            var returns = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .Where(r => r.SalesRepSessionId == id && r.Status == ReturnStatus.Approved && r.IsActive)
                .ToListAsync();

            var grossSales = invoices.Sum(i => i.SubTotal);
            var totalInvoices = invoices.Count;
            var totalCollection = payments.Sum(p => p.Amount);
            var totalReturnAmount = returns.Sum(r => r.TotalAmount);
            var netSales = invoices.Sum(i => i.TotalAmount) - totalReturnAmount;

            var totalVisits = await _unitOfWork.CustomerVisits
                 .GetQueryable()
                 .CountAsync(x => x.SalesRepSessionId == id && x.Status == VisitStatus.Completed && x.IsActive);

            session.GrossSales = grossSales;
            session.NetSales = netSales;
            session.TotalCollection = totalCollection;
            session.TotalReturnAmount = totalReturnAmount;
            session.TotalInvoices = totalInvoices;
            session.TotalVisits = totalVisits;

            session.Status = DayStatus.Closed;
            session.EndTime = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.IsActive = true;



            _unitOfWork.SalesRepSessions.Update(session);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<SalesRepSessionDto>(session);
        }
    }
}
