using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.CustomerVisit.Interfaces;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;
using CustomerVisitEntity = Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;


namespace Salesync.Application.Modules.CustomerVisit.Services
{
    public class CustomerVisitService : ICustomerVisitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<StartCustomerVisitDto> _startValidator;
        private readonly IValidator<CompleteCustomerVisitDto> _completeValidator;
        private readonly ICurrentUserService _currentUser;

        public CustomerVisitService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<StartCustomerVisitDto> startValidator,
            IValidator<CompleteCustomerVisitDto> completeValidator,
            ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _startValidator = startValidator;
            _completeValidator = completeValidator;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<CustomerVisitDto>> GetAllAsync(CustomerVisitFilterDto filter)
        {
            filter ??= new CustomerVisitFilterDto();

            var query = GetCustomerVisitsQuery();

            if (filter.SalesRepId.HasValue)
                query = query.Where(x => x.SalesRepId == filter.SalesRepId.Value);

            if (filter.CustomerId.HasValue)
                query = query.Where(x => x.CustomerId == filter.CustomerId.Value);

            if (filter.RouteId.HasValue)
                query = query.Where(x => x.RouteId == filter.RouteId.Value);

            if (filter.SalesRepSessionId.HasValue)
                query = query.Where(x => x.SalesRepSessionId == filter.SalesRepSessionId.Value);

            if (filter.VisitType.HasValue)
                query = query.Where(x => x.VisitType == filter.VisitType.Value);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(x => x.VisitDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(x => x.VisitDate <= filter.ToDate.Value);

            query = await ApplySalesRepAccessFilterAsync(query);

            var visits = await query
                .OrderByDescending(x => x.VisitDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerVisitDto>>(visits);
        }

        public async Task<CustomerVisitDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid customer visit id.");

            var visit = await GetCustomerVisitsQuery()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (visit == null)
                throw new KeyNotFoundException("Customer visit not found.");

            await EnsureSalesRepCanAccessVisitAsync(visit.SalesRepId);

            return _mapper.Map<CustomerVisitDto>(visit);
        }

        public async Task<IEnumerable<CustomerVisitDto>> GetBySalesRepAsync(int salesRepId)
        {
            if (salesRepId <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            await EnsureSalesRepCanAccessVisitAsync(salesRepId);

            var visits = await GetCustomerVisitsQuery()
                .Where(x => x.SalesRepId == salesRepId)
                .OrderByDescending(x => x.VisitDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerVisitDto>>(visits);
        }

        public async Task<IEnumerable<CustomerVisitDto>> GetByCustomerAsync(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("Invalid customer id.");

            var query = GetCustomerVisitsQuery()
                .Where(x => x.CustomerId == customerId);

            query = await ApplySalesRepAccessFilterAsync(query);

            var visits = await query
                .OrderByDescending(x => x.VisitDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerVisitDto>>(visits);
        }

        public async Task<CustomerVisitDto> StartAsync(StartCustomerVisitDto dto)
        {
            var validationResult = await _startValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var salesRep = await GetVisitSalesRepAsync(dto.SalesRepId);

            await EnsureCustomerExistsAsync(dto.CustomerId);

            if (!dto.RouteId.HasValue)
                throw new InvalidOperationException("Route is required for customer visit.");

            var route = await _unitOfWork.Routes
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dto.RouteId.Value && x.IsActive);

            if (route == null)
                throw new KeyNotFoundException($"Route with id {dto.RouteId.Value} not found.");

            if (route.AssignedSalesRepId.HasValue &&
                route.AssignedSalesRepId.Value != salesRep.Id)
            {
                throw new UnauthorizedAccessException(
                    "Sales rep cannot start a visit on a route assigned to another sales rep.");
            }

            await EnsureCustomerBelongsToRouteAsync(dto.RouteId.Value, dto.CustomerId);

            await EnsureSalesRepSessionIsStartedAsync(dto.SalesRepSessionId, salesRep.Id);

            await EnsureNoActiveVisitAsync(salesRep.Id);

            var visit = _mapper.Map<CustomerVisitEntity>(dto);

            visit.SalesRepId = salesRep.Id;
            visit.RouteId = dto.RouteId.Value;
            visit.VisitDate = DateTime.UtcNow;
            visit.EndTime = null;

            visit.Status = VisitStatus.InProgress;
            visit.VisitType = null;
            visit.NegativeReason = null;

            visit.InvoiceId = null;
            visit.PaymentId = null;
            visit.InvoiceReturnId = null;

            await _unitOfWork.CustomerVisits.AddAsync(visit);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(visit.Id);
        }

        public async Task<CustomerVisitDto> CompleteAsync(int id, CompleteCustomerVisitDto dto)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid customer visit id.");

            var validationResult = await _completeValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var visit = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (visit == null)
                throw new KeyNotFoundException("Customer visit not found.");

            await EnsureSalesRepCanAccessVisitAsync(visit.SalesRepId);

            if (visit.Status != VisitStatus.InProgress)
                throw new InvalidOperationException("Only in-progress visits can be completed.");

            await EnsureLinkedDocumentsMatchVisitAsync(visit, dto);

            visit.VisitType = dto.VisitType;
            visit.NegativeReason = dto.NegativeReason;

            visit.InvoiceId = dto.InvoiceId;
            visit.PaymentId = dto.PaymentId;
            visit.InvoiceReturnId = dto.InvoiceReturnId;

            visit.Status = VisitStatus.Completed;
            visit.EndTime = DateTime.UtcNow;
            visit.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.Notes))
                visit.Notes = dto.Notes;

            _unitOfWork.CustomerVisits.Update(visit);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(visit.Id);
        }

        public async Task CancelAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid customer visit id.");

            var visit = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (visit == null)
                throw new KeyNotFoundException("Customer visit not found.");

            await EnsureSalesRepCanAccessVisitAsync(visit.SalesRepId);

            if (visit.Status != VisitStatus.InProgress)
                throw new InvalidOperationException("Only in-progress visits can be cancelled.");

            visit.Status = VisitStatus.Cancelled;
            visit.EndTime = DateTime.UtcNow;
            visit.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.CustomerVisits.Update(visit);
            await _unitOfWork.CompleteAsync();
        }


        #region Helper Methods

        private IQueryable<CustomerVisitEntity> GetCustomerVisitsQuery()
        {
            return _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.SalesRep)
                .Include(x => x.Customer)
                .Include(x => x.Route)
                .Where(x => x.IsActive);
        }

        private async Task<SalesRepEntity> GetVisitSalesRepAsync(int? dtoSalesRepId)
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

        private async Task EnsureCustomerExistsAsync(int customerId)
        {
            var exists = await _unitOfWork.Customers
                .GetQueryable()
                .AnyAsync(x => x.Id == customerId && x.IsActive);

            if (!exists)
                throw new KeyNotFoundException("Customer not found.");
        }

        private async Task EnsureSalesRepSessionIsStartedAsync(int salesRepSessionId, int salesRepId)
        {
            var sessionExists = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AnyAsync(x =>
                    x.Id == salesRepSessionId &&
                    x.SalesRepId == salesRepId &&
                    x.Status == DayStatus.Started &&
                    x.IsActive);

            if (!sessionExists)
                throw new InvalidOperationException("Sales rep session must be started and belong to the same sales rep.");
        }

        private async Task EnsureNoActiveVisitAsync(int salesRepId)
        {
            var hasActiveVisit = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AnyAsync(x =>
                    x.SalesRepId == salesRepId &&
                    x.Status == VisitStatus.InProgress &&
                    x.IsActive);

            if (hasActiveVisit)
                throw new InvalidOperationException("Sales rep already has an active visit.");
        }

        private async Task EnsureLinkedDocumentsMatchVisitAsync(
            CustomerVisitEntity visit,
            CompleteCustomerVisitDto dto)
        {
            if (dto.VisitType == VisitType.Negative)
                return;

            if (dto.InvoiceId.HasValue)
            {
                var invoiceExists = await _unitOfWork.Invoices
                    .GetQueryable()
                    .AnyAsync(x =>
                        x.Id == dto.InvoiceId.Value &&
                        x.CustomerId == visit.CustomerId &&
                        x.SalesRepId == visit.SalesRepId &&
                        x.SalesRepSessionId == visit.SalesRepSessionId &&
                        x.IsActive);

                if (!invoiceExists)
                    throw new KeyNotFoundException("Invoice not found or does not match this visit.");
            }

            if (dto.PaymentId.HasValue)
            {
                var paymentExists = await _unitOfWork.Payments
                    .GetQueryable()
                    .AnyAsync(x =>
                        x.Id == dto.PaymentId.Value &&
                        x.CustomerId == visit.CustomerId &&
                        x.SalesRepId == visit.SalesRepId &&
                        x.SalesRepSessionId == visit.SalesRepSessionId &&
                        x.IsActive);

                if (!paymentExists)
                    throw new KeyNotFoundException("Payment not found or does not match this visit.");
            }

            if (dto.InvoiceReturnId.HasValue)
            {
                var returnExists = await _unitOfWork.InvoiceReturns
                    .GetQueryable()
                    .AnyAsync(x =>
                        x.Id == dto.InvoiceReturnId.Value &&
                        x.CustomerId == visit.CustomerId &&
                        x.SalesRepId == visit.SalesRepId &&
                        x.SalesRepSessionId == visit.SalesRepSessionId &&
                        x.IsActive);

                if (!returnExists)
                    throw new KeyNotFoundException("Invoice return not found or does not match this visit.");
            }
        }

        private async Task EnsureSalesRepCanAccessVisitAsync(int salesRepId)
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
                throw new UnauthorizedAccessException("You are not allowed to access this visit.");
        }

        private async Task<IQueryable<CustomerVisitEntity>> ApplySalesRepAccessFilterAsync(IQueryable<CustomerVisitEntity> query)
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

        private async Task EnsureCustomerBelongsToRouteAsync(int routeId, int customerId)
        {
            var customerExistsInRoute = await _unitOfWork.RouteCustomers
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.RouteId == routeId &&
                    x.CustomerId == customerId &&
                    x.IsActive);

            if (!customerExistsInRoute)
                throw new InvalidOperationException("Customer does not belong to the selected route.");
        }

        #endregion
    }
}