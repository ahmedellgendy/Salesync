using AutoMapper;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;
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
            var existingSession = await _unitOfWork.SalesRepSessions
                .FindAsync(s => s.SalesRepId == dto.SalesRepId && s.WorkingDate == DateTime.UtcNow.Date && s.Status != DayStatus.Closed);

            if (existingSession.Any())
                throw new InvalidOperationException($"You already have a session today.");

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

            session.Status = DayStatus.Closed;
            session.EndTime = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.SalesRepSessions.Update(session);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<SalesRepSessionDto>(session);
        }
    }
}
