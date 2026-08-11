using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.CustomerVisit.Interfaces;
using Salesync.Application.Modules.Sales.Dtos.Invoice;
using Salesync.Application.Modules.Sales.Dtos.InvoiceItem;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.UnloadRequest.Dtos;
using Salesync.Application.Modules.UnloadRequest.Interfaces;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;
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
        private readonly IInvoiceService _invoiceService;
        private readonly ISalesRepUnloadRequestService _unloadRequestService;
        private readonly IInvoiceReturnService _invoiceReturnService;

        public SalesRepMobileService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUser,
            ISalesRepSessionService salesRepSessionService,
            ICustomerVisitService customerVisitService,
            IPaymentService paymentService,
            IInvoiceService invoiceService ,
            ISalesRepUnloadRequestService unloadRequestService,
            IInvoiceReturnService invoiceReturnService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _salesRepSessionService = salesRepSessionService;
            _customerVisitService = customerVisitService;
            _paymentService = paymentService;
            _invoiceService = invoiceService;
            _unloadRequestService = unloadRequestService;
            _invoiceReturnService = invoiceReturnService;
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
                BusinessUnitId = salesRep.BusinessUnitId,
                ProfileImageUrl = salesRep.ProfileImageUrl
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

            var routeCustomers = await GetAssignedRouteCustomersAsync(salesRep.Id);

            var visits = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.SalesRepSessionId == session.Id &&
                    x.IsActive)
                .ToListAsync();

            var invoices = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.SalesRepSessionId == session.Id &&
                    x.IsActive)
                .ToListAsync();

            var payments = await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.SalesRepSessionId == session.Id &&
                    x.IsActive)
                .ToListAsync();

            var totalCustomers = routeCustomers
                .Select(x => x.CustomerId)
                .Distinct()
                .Count();

            var visitedCustomers = visits
                .Select(x => x.CustomerId)
                .Distinct()
                .Count();

            var remainingCustomers = totalCustomers - visitedCustomers;

            if (remainingCustomers < 0)
                remainingCustomers = 0;

            var salesTotal = invoices.Sum(x => x.TotalAmount);
            var paidTotal = payments.Sum(x => x.Amount);
            var remainingTotal = invoices.Sum(x => x.TotalAmount - x.PaidAmount);

            if (remainingTotal < 0)
                remainingTotal = 0;

            return new SalesRepMobileTodayDto
            {
                HasTodaySession = true,
                HasOpenSession = !isDayClosed,
                IsDayClosed = isDayClosed,
                Session = _mapper.Map<SalesRepSessionDto>(session),

                TotalCustomers = totalCustomers,
                VisitedCustomers = visitedCustomers,
                RemainingCustomers = remainingCustomers,

                InvoicesCount = invoices.Count,
                SalesTotal = salesTotal,

                PaidTotal = paidTotal,
                RemainingTotal = remainingTotal,

                PaymentsCount = payments.Count
            };
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
        public async Task<IEnumerable<SalesRepMobileInvoiceDto>> GetInvoicesAsync(int sessionId)
        {
            if (sessionId <= 0)
                throw new ArgumentException("Invalid session id.");

            var salesRep = await GetCurrentSalesRepAsync();

            await EnsureSessionBelongsToSalesRepAsync(sessionId, salesRep.Id, allowClosedSession: true);

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
        public async Task<InvoiceDto> GetInvoiceDetailsAsync(int invoiceId)
        {
            if (invoiceId <= 0)
                throw new ArgumentException("Invalid invoice id.");

            var salesRep = await GetCurrentSalesRepAsync();

            var invoice = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.SalesRep)
                .Include(x => x.InvoiceItems)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x =>
                    x.Id == invoiceId &&
                    x.IsActive &&
                    x.SalesRepId == salesRep.Id);

            if (invoice is null)
                throw new KeyNotFoundException("Invoice not found for current sales rep.");

            var dto = _mapper.Map<InvoiceDto>(invoice);

            dto.CustomerCode = invoice.CustomerId.ToString();

            if (invoice.Customer is not null)
            {
                dto.CustomerName = invoice.Customer.Name;
                dto.CustomerPhone = invoice.Customer.Phone;
                dto.CustomerAddress = invoice.Customer.Address;
            }

            dto.SalesRepCode = salesRep.SalesRepCode;
            dto.SalesRepName = salesRep.Name;
            dto.SalesRepPhone = salesRep.Phone;

            dto.RemainingAmount = dto.TotalAmount - dto.PaidAmount;
            dto.DueDate = dto.DueDate ?? invoice.CreatedAt;
            dto.ReturnsAmount = dto.ReturnsAmount < 0 ? 0 : dto.ReturnsAmount;

            return dto;
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
        public async Task<IEnumerable<SalesRepMobileRouteDto>> GetRoutesAsync(int sessionId)
        {
            if (sessionId <= 0)
                throw new ArgumentException("Invalid session id.");

            var salesRep = await GetCurrentSalesRepAsync();

            await EnsureSessionBelongsToSalesRepAsync(sessionId, salesRep.Id);

            var routeCustomers = await GetAssignedRouteCustomersAsync(salesRep.Id);

            var visits = await GetSessionVisitsAsync(sessionId, salesRep.Id);

            var hasActiveVisit = visits.Any(x => x.Status == (int)VisitStatus.InProgress);

            var visitedCustomerIds = visits
                .Select(x => x.CustomerId)
                .Distinct()
                .ToHashSet();

            var routes = routeCustomers
                .GroupBy(x => new
                {
                    x.RouteId,
                    x.RouteCode,
                    x.RouteName
                })
                .Select(g =>
                {
                    var totalCustomers = g.Count();
                    var visitedCustomers = g.Count(x => visitedCustomerIds.Contains(x.CustomerId));
                    var remainingCustomers = totalCustomers - visitedCustomers;

                    return new SalesRepMobileRouteDto
                    {
                        RouteId = g.Key.RouteId,
                        RouteCode = g.Key.RouteCode,
                        RouteName = g.Key.RouteName,
                        TotalCustomers = totalCustomers,
                        VisitedCustomers = visitedCustomers,
                        RemainingCustomers = remainingCustomers,
                        HasActiveVisit = hasActiveVisit,
                        CanOpen = !hasActiveVisit
                    };
                })
                .OrderBy(x => x.RouteName)
                .ToList();

            return routes;
        }
        public async Task<InvoiceDto> CreateInvoiceAsync(CreateSalesRepMobileInvoiceDto dto)
        {
            if (dto.VisitId <= 0)
                throw new ArgumentException("Invalid visit id.");

            if (dto.CustomerId <= 0)
                throw new ArgumentException("Invalid customer id.");

            if (dto.SalesRepSessionId <= 0)
                throw new ArgumentException("Invalid session id.");

            if (dto.Items is null || dto.Items.Count == 0)
                throw new ArgumentException("Invoice must contain at least one item.");

            var salesRep = await GetCurrentSalesRepAsync();

            await EnsureSessionBelongsToSalesRepAsync(
                dto.SalesRepSessionId,
                salesRep.Id);

            var visit = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.VisitId &&
                    x.CustomerId == dto.CustomerId &&
                    x.SalesRepId == salesRep.Id &&
                    x.SalesRepSessionId == dto.SalesRepSessionId &&
                    x.IsActive);

            if (visit is null)
                throw new KeyNotFoundException("Visit not found for current sales rep session.");

            if (visit.Status != VisitStatus.InProgress)
                throw new InvalidOperationException("Visit must be in progress to create invoice.");

            await EnsureInvoiceItemsAvailableAsync(
                salesRep.Id,
                dto.Items);

            var warehouse = await _unitOfWork.Warehouses
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.BranchId == salesRep.BranchId &&
                    x.IsActive);

            if (warehouse is null)
                throw new KeyNotFoundException("No active warehouse found for current sales rep branch.");

            var createInvoiceDto = new CreateInvoiceDto
            {
                CustomerId = dto.CustomerId,
                WarehouseId = warehouse.Id,
                SalesRepSessionId = dto.SalesRepSessionId,
                SalesChannel = (SalesChannel)1,
                DiscountAmount = dto.DiscountAmount,
                Notes = dto.Notes,
                Items = dto.Items.Select(x => new CreateInvoiceItemDto
                {
                    ProductId = x.ProductId,
                    SaleLargeQuantity = x.SaleLargeQuantity,
                    BonusLargeQuantity = x.BonusLargeQuantity,
                    DiscountAmount = x.DiscountAmount,
                    DiscountPercentage = x.DiscountPercentage
                }).ToList()
            };

            var invoice = await _invoiceService.CreateAsync(createInvoiceDto);

            var confirmedInvoice = await _invoiceService.ConfirmAsync(invoice.Id);

            var completeVisitDto = new CompleteSalesRepMobileVisitDto
            {
                VisitType = (VisitType)1,
                NegativeReason = null,
                InvoiceId = confirmedInvoice.Id,
                PaymentId = null,
                InvoiceReturnId = null,
                Notes = dto.Notes
            };

            await CompleteVisitAsync(dto.VisitId, completeVisitDto);

            return confirmedInvoice;
        }

        public async Task<IEnumerable<MobileProductOptionDto>> GetProductsAsync()
        {
            var products = await _unitOfWork.Products
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new MobileProductOptionDto
                {
                    Id = x.Id,
                    ItemCode = x.ItemCode,
                    Name = x.Name,
                    UnitPrice = x.UnitPrice,
                    Unit = x.Unit,
                    SmallUnit = x.SmallUnit,
                    LargeUnit = x.LargeUnit,
                    UnitsPerLargeUnit = x.UnitsPerLargeUnit
                })
                .ToListAsync();

            return products;
        }
        public async Task<IEnumerable<MobileWarehouseOptionDto>> GetWarehousesAsync()
        {
            var salesRep = await GetCurrentSalesRepAsync();

            var warehouses = await _unitOfWork.Warehouses
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.BranchId == salesRep.BranchId)
                .OrderBy(x => x.Name)
                .Select(x => new MobileWarehouseOptionDto
                {
                    Id = x.Id,
                    WarehouseCode = x.WarehouseCode,
                    Name = x.Name,
                    BranchId = x.BranchId,
                    Location = x.Location
                })
                .ToListAsync();

            return warehouses;
        }

        #region Invoice Returns

        public async Task<IEnumerable<InvoiceReturnDto>> GetMyReturnsAsync()
        {
            var salesRep = await GetCurrentSalesRepAsync();

            return await _invoiceReturnService
                .GetBySalesRepIdAsync(salesRep.Id);
        }

        public async Task<InvoiceReturnDto> GetReturnByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid return id.");

            var salesRep = await GetCurrentSalesRepAsync();

            var invoiceReturn =
                await _invoiceReturnService.GetByIdAsync(id);

            if (invoiceReturn.SalesRepId != salesRep.Id)
                throw new UnauthorizedAccessException(
                    "Return does not belong to current sales rep.");

            return invoiceReturn;
        }

        public async Task<InvoiceReturnDto> CreateReturnAsync(CreateInvoiceReturnDto dto)
        {
            if (dto.InvoiceId <= 0)
                throw new ArgumentException("Invalid invoice id.");

            if (dto.Items is null || dto.Items.Count == 0)
                throw new ArgumentException(
                    "Return must contain at least one item.");

            var salesRep = await GetCurrentSalesRepAsync();

            var currentSession =await GetCurrentOpenSessionAsync(salesRep.Id);

            var invoice = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.InvoiceId &&
                    x.IsActive &&
                    x.SalesRepId == salesRep.Id);

            if (invoice is null)
                throw new KeyNotFoundException("Invoice not found for current sales rep.");

            dto.SalesRepId = salesRep.Id;
            dto.SalesRepSessionId = currentSession.Id;

            return await _invoiceReturnService.CreateAsync(dto);
        }

        public async Task<InvoiceReturnDto> CancelReturnAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid return id.");

            var salesRep = await GetCurrentSalesRepAsync();

            var invoiceReturn =
                await _invoiceReturnService.GetByIdAsync(id);

            if (invoiceReturn.SalesRepId != salesRep.Id)
                throw new UnauthorizedAccessException(
                    "Return does not belong to current sales rep.");

            return await _invoiceReturnService
                .CancelAsync(id);
        }

        public async Task<IEnumerable<MobileReturnableInvoiceDto>>GetReturnableInvoicesAsync(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("Invalid customer id.");

            var salesRep = await GetCurrentSalesRepAsync();

            return await _invoiceReturnService
                .GetReturnableInvoicesAsync(
                    salesRep.Id,
                    customerId);
        }

        public async Task<MobileReturnableInvoiceDetailsDto>GetReturnableInvoiceDetailsAsync(int invoiceId)
        {
            if (invoiceId <= 0)
                throw new ArgumentException("Invalid invoice id.");

            var salesRep = await GetCurrentSalesRepAsync();

            return await _invoiceReturnService
                .GetReturnableInvoiceDetailsAsync(
                    salesRep.Id,
                    invoiceId);
        }

        #endregion

        #region Unload Requests

        public async Task<SalesRepUnloadRequestDto> CreateUnloadRequestAsync(
            CreateSalesRepUnloadRequestDto dto)
        {
            return await _unloadRequestService.CreateAsync(dto);
        }

        public async Task<IEnumerable<SalesRepUnloadRequestDto>> GetMyUnloadRequestsAsync()
        {
            return await _unloadRequestService.GetMyRequestsAsync();
        }

        public async Task<SalesRepUnloadRequestDto> GetUnloadRequestByIdAsync(int id)
        {
            return await _unloadRequestService.GetMyRequestByIdAsync(id);
        }

        public async Task CancelUnloadRequestAsync(int id, string? reason)
        {
            await _unloadRequestService.CancelMyRequestAsync(id, reason);
        }

        #endregion


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
        private async Task<SalesRepSession> GetCurrentOpenSessionAsync(int salesRepId)
        {
            var today = DateTime.UtcNow.Date;

            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId == salesRepId &&
                    x.WorkingDate == today &&
                    x.IsActive &&
                    !x.EndTime.HasValue)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (session is null)
                throw new InvalidOperationException(
                    "No open sales rep session found for today.");

            if (session.IsStockSettled)
                throw new InvalidOperationException(
                    "Cannot create return after stock has been settled.");

            return session;
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
        private async Task<SalesRepSession> EnsureSessionBelongsToSalesRepAsync(int sessionId, int salesRepId, bool allowClosedSession = false)
        {
            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == sessionId &&
                    x.SalesRepId == salesRepId &&
                    x.IsActive);

            if (session is null)
                throw new KeyNotFoundException("Session not found for current sales rep.");

            var isClosed = session.EndTime.HasValue;

            if (isClosed && !allowClosedSession)
                throw new InvalidOperationException("Cannot perform this action for a closed session.");

            return session;
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
        public async Task<IEnumerable<SalesRepMobileCustomerDto>> GetRouteCustomersAsync(int sessionId, int routeId)
        {
            if (sessionId <= 0)
                throw new ArgumentException("Invalid session id.");

            if (routeId <= 0)
                throw new ArgumentException("Invalid route id.");

            var salesRep = await GetCurrentSalesRepAsync();

            await EnsureSessionBelongsToSalesRepAsync(sessionId, salesRep.Id);

            var routeCustomers = await GetAssignedRouteCustomersAsync(salesRep.Id);

            var selectedRouteCustomers = routeCustomers
                .Where(x => x.RouteId == routeId)
                .ToList();

            if (selectedRouteCustomers.Count == 0)
                throw new KeyNotFoundException("Route not found for current sales rep.");

            var visits = await GetSessionVisitsAsync(sessionId, salesRep.Id);

            return BuildMobileCustomers(selectedRouteCustomers, visits);
        }

        private async Task EnsureInvoiceItemsAvailableAsync(int salesRepId, List<CreateSalesRepMobileInvoiceItemDto> items)
        {
            foreach (var item in items)
            {
                if (item.ProductId <= 0)
                    throw new ArgumentException("Invalid product id.");

                if (item.SaleLargeQuantity <= 0)
                    throw new ArgumentException("Item quantity must be greater than zero.");

                if (item.BonusLargeQuantity < 0)
                    throw new ArgumentException("Bonus quantity cannot be negative.");

                if (item.DiscountAmount < 0)
                    throw new ArgumentException("Discount amount cannot be negative.");

                if (item.DiscountPercentage < 0 || item.DiscountPercentage > 100)
                    throw new ArgumentException("Discount percentage must be between 0 and 100.");
            }

            var productIds = items
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = await _unitOfWork.Products
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    productIds.Contains(x.Id) &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.SmallUnit,
                    x.LargeUnit,
                    x.UnitsPerLargeUnit
                })
                .ToListAsync();

            foreach (var productId in productIds)
            {
                if (!products.Any(x => x.Id == productId))
                    throw new KeyNotFoundException($"Product with id {productId} not found or inactive.");
            }

            var inventories = await _unitOfWork.SalesRepInventories
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId == salesRepId &&
                    productIds.Contains(x.ProductId) &&
                    x.IsActive)
                .ToListAsync();

            foreach (var group in items.GroupBy(x => x.ProductId))
            {
                var product = products.First(x => x.Id == group.Key);

                var unitsPerLargeUnit = product.UnitsPerLargeUnit <= 0
                    ? 1
                    : product.UnitsPerLargeUnit;

                var requestedSmallQuantity = group.Sum(x =>
                    (x.SaleLargeQuantity + x.BonusLargeQuantity) * unitsPerLargeUnit);

                var inventory = inventories
                    .FirstOrDefault(x => x.ProductId == group.Key);

                if (inventory is null)
                {
                    throw new InvalidOperationException(
                        $"No sales rep inventory found for product {product.Name}.");
                }

                if (inventory.Quantity < requestedSmallQuantity)
                {
                    var largeUnit = string.IsNullOrWhiteSpace(product.LargeUnit)
                        ? "كرتونة"
                        : product.LargeUnit;

                    var smallUnit = string.IsNullOrWhiteSpace(product.SmallUnit)
                        ? "قطعة"
                        : product.SmallUnit;

                    var requestedLargeQuantity = group.Sum(x =>
                        x.SaleLargeQuantity + x.BonusLargeQuantity);

                    throw new InvalidOperationException(
                        $"Requested quantity for product {product.Name} is greater than available quantity. " +
                        $"Requested: {requestedLargeQuantity} {largeUnit} = {requestedSmallQuantity} {smallUnit}, " +
                        $"Available: {inventory.Quantity} {smallUnit}.");
                }
            }
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
