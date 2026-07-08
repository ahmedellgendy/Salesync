using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.CustomerVisit.Interfaces;
using Salesync.Domain.Common.Enums.CustomerVisit;
using CustomerVisitEntity = Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;


namespace Salesync.Application.Modules.CustomerVisit.Services
{
    public class CustomerVisitService : ICustomerVisitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateCustomerVisitDto> _createValidator;
        private readonly ICurrentUserService _currentUser;

        public CustomerVisitService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateCustomerVisitDto> createValidator,
            ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
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

        public async Task<CustomerVisitDto> CreateAsync(CreateCustomerVisitDto dto)
        {
            var validationResult = await _createValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var salesRep = await GetVisitSalesRepAsync(dto.SalesRepId);

            await EnsureCustomerExistsAsync(dto.CustomerId);

            if (dto.RouteId.HasValue)
                await EnsureRouteExistsAsync(dto.RouteId.Value);

            if (dto.SalesRepSessionId.HasValue)
                await EnsureSalesRepSessionExistsAsync(dto.SalesRepSessionId.Value, salesRep.Id);

            await EnsurePositiveVisitLinksExistAsync(dto);

            var visit = _mapper.Map<CustomerVisitEntity>(dto);

            visit.SalesRepId = salesRep.Id;
            visit.VisitDate = DateTime.UtcNow;
            visit.Status = VisitStatus.Completed;

            await _unitOfWork.CustomerVisits.AddAsync(visit);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(visit.Id);
        }

        public async Task CancelAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid customer visit id.");

            var visit = await _unitOfWork.CustomerVisits.GetByIdAsync(id);

            if (visit == null || !visit.IsActive)
                throw new KeyNotFoundException("Customer visit not found.");

            await EnsureSalesRepCanAccessVisitAsync(visit.SalesRepId);

            if (visit.Status == VisitStatus.Cancelled)
                throw new InvalidOperationException("Customer visit is already cancelled.");

            visit.Status = VisitStatus.Cancelled;
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
            var customerExists = await _unitOfWork.Customers
                .GetQueryable()
                .AnyAsync(x => x.Id == customerId && x.IsActive);

            if (!customerExists)
                throw new KeyNotFoundException("Customer not found.");
        }

        private async Task EnsureRouteExistsAsync(int routeId)
        {
            var routeExists = await _unitOfWork.Routes
                .GetQueryable()
                .AnyAsync(x => x.Id == routeId && x.IsActive);

            if (!routeExists)
                throw new KeyNotFoundException("Route not found.");
        }

        private async Task EnsureSalesRepSessionExistsAsync(int salesRepSessionId, int salesRepId)
        {
            var sessionExists = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AnyAsync(x =>
                    x.Id == salesRepSessionId &&
                    x.SalesRepId == salesRepId &&
                    x.IsActive);

            if (!sessionExists)
                throw new KeyNotFoundException("Sales rep session not found.");
        }

        private async Task EnsurePositiveVisitLinksExistAsync(CreateCustomerVisitDto dto)
        {
            if (dto.VisitType == VisitType.Negative)
                return;

            if (dto.InvoiceId.HasValue)
            {
                var invoiceExists = await _unitOfWork.Invoices
                    .GetQueryable()
                    .AnyAsync(x =>
                        x.Id == dto.InvoiceId.Value &&
                        x.IsActive);

                if (!invoiceExists)
                    throw new KeyNotFoundException("Invoice not found.");
            }

            if (dto.PaymentId.HasValue)
            {
                var paymentExists = await _unitOfWork.Payments
                    .GetQueryable()
                    .AnyAsync(x =>
                        x.Id == dto.PaymentId.Value &&
                        x.IsActive);

                if (!paymentExists)
                    throw new KeyNotFoundException("Payment not found.");
            }

            if (dto.InvoiceReturnId.HasValue)
            {
                var invoiceReturnExists = await _unitOfWork.InvoiceReturns
                    .GetQueryable()
                    .AnyAsync(x =>
                        x.Id == dto.InvoiceReturnId.Value &&
                        x.IsActive);

                if (!invoiceReturnExists)
                    throw new KeyNotFoundException("Invoice return not found.");
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


        #endregion
    }
}