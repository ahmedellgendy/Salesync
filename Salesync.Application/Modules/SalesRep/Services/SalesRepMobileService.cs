using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.CustomerVisit.Interfaces;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Application.Modules.SalesRep.Services
{
    public class SalesRepMobileService : ISalesRepMobileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;
        private readonly ISalesRepSessionService _salesRepSessionService;
        private readonly ICustomerVisitService _customerVisitService;
        private readonly IPaymentService _paymentService;

        public SalesRepMobileService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUser,
            ISalesRepSessionService salesRepSessionService,
            ICustomerVisitService customerVisitService,
            IPaymentService paymentService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _salesRepSessionService = salesRepSessionService;
            _customerVisitService = customerVisitService;
            _paymentService = paymentService;
        }

        public async Task<SalesRepMobileProfileDto> GetProfileAsync()
        {
            var salesRep = await GetCurrentSalesRepAsync();

            var branchName = await _unitOfWork.Branches
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.Id == salesRep.BranchId && x.IsActive)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            return new SalesRepMobileProfileDto
            {
                SalesRepId = salesRep.Id,
                SalesRepCode = salesRep.SalesRepCode,
                Name = salesRep.Name,
                Phone = salesRep.Phone,
                Mobile = salesRep.Mobile,
                Email = salesRep.Email,
                BranchId = salesRep.BranchId,
                BranchName = branchName,
                BusinessUnitId = salesRep.BusinessUnitId
            };
        }
        public async Task<SalesRepMobileTodayDto> GetTodayAsync()
        {
            var salesRep = await GetCurrentSalesRepAsync();

            var today = DateTime.UtcNow.Date;

            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.WorkingDate == today &&
                    x.IsActive)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (session is null)
            {
                return new SalesRepMobileTodayDto
                {
                    HasTodaySession = false,
                    HasOpenSession = false,
                    IsDayClosed = false,
                    Session = null
                };
            }

            var isDayClosed = session.EndTime.HasValue;

            return new SalesRepMobileTodayDto
            {
                HasTodaySession = true,
                HasOpenSession = !isDayClosed,
                IsDayClosed = isDayClosed,
                Session = _mapper.Map<SalesRepSessionDto>(session)
            };
        }
        public async Task<IEnumerable<SalesRepMobileCustomerDto>> GetCustomersAsync(int sessionId)
        {
            if (sessionId <= 0)
                throw new ArgumentException("Invalid session id.");

            var salesRep = await GetCurrentSalesRepAsync();

            await EnsureSessionBelongsToSalesRepAsync(sessionId, salesRep.Id);

            var routeCustomers = await GetAssignedRouteCustomersAsync(salesRep.Id);

            var visits = await GetSessionVisitsAsync(sessionId, salesRep.Id);

            return BuildMobileCustomers(routeCustomers, visits);
        }
        public async Task<SalesRepSessionDto> StartDayAsync(StartSalesRepMobileDayDto dto)
        {
            var salesRep = await GetCurrentSalesRepAsync();

            var createSessionDto = new CreateSalesRepSessionDto
            {
                SalesRepId = salesRep.Id
            };

            return await _salesRepSessionService.StartSessionAsync(createSessionDto);
        }
        public async Task<SalesRepSessionDto> CloseDayAsync(int sessionId)
        {
            if (sessionId <= 0)
                throw new ArgumentException("Invalid session id.");

            await GetCurrentSalesRepAsync();

            return await _salesRepSessionService.CloseSessionAsync(sessionId);
        }
        public async Task<IEnumerable<SalesRepMobileInvoiceDto>> GetInvoicesAsync(int sessionId)
        {
            if (sessionId <= 0)
                throw new ArgumentException("Invalid session id.");

            var salesRep = await GetCurrentSalesRepAsync();

            await EnsureSessionBelongsToSalesRepAsync(sessionId, salesRep.Id);

            var invoices =
                await
                (
                    from invoice in _unitOfWork.Invoices.GetQueryable().AsNoTracking()
                    join customer in _unitOfWork.Customers.GetQueryable().AsNoTracking()
                        on invoice.CustomerId equals customer.Id
                    where invoice.IsActive
                          && customer.IsActive
                          && invoice.SalesRepId == salesRep.Id
                          && invoice.SalesRepSessionId == sessionId
                    orderby invoice.CreatedAt descending
                    select new SalesRepMobileInvoiceDto
                    {
                        Id = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        CustomerId = invoice.CustomerId,
                        CustomerName = customer.Name,
                        WarehouseId = invoice.WarehouseId,
                        SalesRepSessionId = invoice.SalesRepSessionId,
                        Status = (int)invoice.Status,
                        PaymentStatus = (int)invoice.PaymentStatus,
                        SubTotal = invoice.SubTotal,
                        DiscountAmount = invoice.DiscountAmount,
                        TaxAmount = invoice.TaxAmount,
                        TotalAmount = invoice.TotalAmount,
                        PaidAmount = invoice.PaidAmount,
                        RemainingAmount = invoice.TotalAmount - invoice.PaidAmount,
                        CreatedAt = invoice.CreatedAt,
                        Notes = invoice.Notes
                    }
                ).ToListAsync();

            return invoices;
        }
        public async Task<CustomerVisitDto> StartVisitAsync(StartSalesRepMobileVisitDto dto)
        {
            var startVisitDto = new StartCustomerVisitDto
            {
                SalesRepId = null,
                CustomerId = dto.CustomerId,
                RouteId = dto.RouteId,
                SalesRepSessionId = dto.SalesRepSessionId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Notes = dto.Notes
            };

            return await _customerVisitService.StartAsync(startVisitDto);
        }
        public async Task<CustomerVisitDto> CompleteVisitAsync(int visitId, CompleteSalesRepMobileVisitDto dto)
        {
            if (visitId <= 0)
                throw new ArgumentException("Invalid visit id.");

            var completeVisitDto = new CompleteCustomerVisitDto
            {
                VisitType = dto.VisitType,
                NegativeReason = dto.NegativeReason,
                InvoiceId = dto.InvoiceId,
                PaymentId = dto.PaymentId,
                InvoiceReturnId = dto.InvoiceReturnId,
                Notes = dto.Notes
            };

            return await _customerVisitService.CompleteAsync(visitId, completeVisitDto);
        }

        public async Task<PaymentDto> CreatePaymentAsync(CreateSalesRepMobilePaymentDto dto)
        {
            if (dto.InvoiceId <= 0)
                throw new ArgumentException("Invalid invoice id.");

            if (dto.SalesRepSessionId <= 0)
                throw new ArgumentException("Invalid session id.");

            if (dto.Amount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero.");

            var salesRep = await GetCurrentSalesRepAsync();

            await EnsureSessionBelongsToSalesRepAsync(dto.SalesRepSessionId, salesRep.Id);

            var invoice = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.InvoiceId &&
                    x.IsActive &&
                    x.SalesRepId == salesRep.Id &&
                    x.SalesRepSessionId == dto.SalesRepSessionId);

            if (invoice is null)
                throw new KeyNotFoundException("Invoice not found for current sales rep session.");

            var createPaymentDto = new CreatePaymentDto
            {
                InvoiceId = invoice.Id,
                SalesRepId = salesRep.Id,
                SalesRepSessionId = dto.SalesRepSessionId,
                Amount = dto.Amount,
                PaymentMethod = (PaymentMethod)dto.PaymentMethod,
                Notes = dto.Notes
            };

            return await _paymentService.CreateAsync(createPaymentDto);
        }

        #region Helper Method

        private async Task<SalesRepEntity> GetCurrentSalesRepAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == _currentUser.UserId &&
                    x.IsActive);

            if (salesRep == null)
                throw new UnauthorizedAccessException("Sales rep not found for current user.");

            return salesRep;
        }

        private async Task EnsureSessionBelongsToSalesRepAsync(int sessionId, int salesRepId)
        {
            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == sessionId &&
                    x.SalesRepId == salesRepId &&
                    x.IsActive);

            if (session == null)
                throw new UnauthorizedAccessException("Session does not belong to the current sales rep.");

            if (session.Status == DayStatus.Closed || session.EndTime.HasValue)
                throw new InvalidOperationException("Cannot load customers for a closed session.");
        }

        private async Task<List<RouteCustomerMobileProjection>> GetAssignedRouteCustomersAsync(int salesRepId)
        {
            return await (
                from routeCustomer in _unitOfWork.RouteCustomers.GetQueryable().AsNoTracking()
                join route in _unitOfWork.Routes.GetQueryable().AsNoTracking()
                    on routeCustomer.RouteId equals route.Id
                join customer in _unitOfWork.Customers.GetQueryable().AsNoTracking()
                    on routeCustomer.CustomerId equals customer.Id
                where routeCustomer.IsActive
                      && route.IsActive
                      && customer.IsActive
                      && route.AssignedSalesRepId == salesRepId
                orderby routeCustomer.VisitSequence
                select new RouteCustomerMobileProjection
                {
                    RouteCustomerId = routeCustomer.Id,

                    RouteId = route.Id,
                    RouteCode = route.RouteCode,
                    RouteName = route.Name,

                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    Area = customer.Area,
                    City = customer.City,
                    Latitude = customer.Latitude,
                    Longitude = customer.Longitude,
                    CurrentBalance = customer.CurrentBalance,
                    CreditLimit = customer.CreditLimit,

                    VisitSequence = routeCustomer.VisitSequence,
                    VisitDays = routeCustomer.VisitDays
                })
                .ToListAsync();
        }

        private async Task<List<CustomerVisitMobileProjection>> GetSessionVisitsAsync(int sessionId, int salesRepId)
        {
            return await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId == salesRepId &&
                    x.SalesRepSessionId == sessionId &&
                    x.IsActive)
                .Select(x => new CustomerVisitMobileProjection
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    Status = (int)x.Status,
                    VisitType = x.VisitType.HasValue ? (int?)x.VisitType.Value : null,
                    VisitDate = x.VisitDate
                })
                .ToListAsync();
        }

        private static IEnumerable<SalesRepMobileCustomerDto> BuildMobileCustomers(List<RouteCustomerMobileProjection> routeCustomers, List<CustomerVisitMobileProjection> visits)
        {
            var latestVisitByCustomer = visits
                .GroupBy(x => x.CustomerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.VisitDate).First());

            var hasActiveVisit = visits.Any(x => x.Status == (int)VisitStatus.InProgress);

            return routeCustomers.Select(x =>
            {
                latestVisitByCustomer.TryGetValue(x.CustomerId, out var visit);

                return new SalesRepMobileCustomerDto
                {
                    RouteCustomerId = x.RouteCustomerId,

                    RouteId = x.RouteId,
                    RouteCode = x.RouteCode,
                    RouteName = x.RouteName,

                    CustomerId = x.CustomerId,
                    CustomerName = x.CustomerName,
                    Phone = x.Phone,
                    Address = x.Address,
                    Area = x.Area,
                    City = x.City,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    CurrentBalance = x.CurrentBalance,
                    CreditLimit = x.CreditLimit,

                    VisitSequence = x.VisitSequence,
                    VisitDays = x.VisitDays,

                    VisitId = visit?.Id,
                    VisitStatus = visit?.Status,
                    VisitType = visit?.VisitType,

                    HasVisitToday = visit != null,
                    CanStartVisit = visit == null && !hasActiveVisit
                };
            });
        }

        #endregion

        #region projection classes

        private sealed class RouteCustomerMobileProjection
        {
            public int RouteCustomerId { get; set; }
            public int RouteId { get; set; }
            public string RouteCode { get; set; } = string.Empty;
            public string RouteName { get; set; } = string.Empty;
            public int CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? Area { get; set; }
            public string? City { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public decimal? CurrentBalance { get; set; }
            public decimal? CreditLimit { get; set; }
            public int VisitSequence { get; set; }
            public string? VisitDays { get; set; }
        }
        private sealed class CustomerVisitMobileProjection
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public int Status { get; set; }
            public int? VisitType { get; set; }
            public DateTime VisitDate { get; set; }
        }


        #endregion
    }
}
