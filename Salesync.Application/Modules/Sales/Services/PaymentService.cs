using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Application.Modules.Sales.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreatePaymentDto> _createPaymentValidator;
        private readonly ICurrentUserService _currentUser;

        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreatePaymentDto> createPaymentValidator, ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createPaymentValidator = createPaymentValidator;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<PaymentDto>> GetAllAsync()
        {
            var payments = await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Invoice)
                .Include(x => x.Customer)
                .Include(x => x.SalesRep)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            var result = _mapper.Map<List<PaymentDto>>(payments);

            for (var i = 0; i < payments.Count; i++)
            {
                PopulatePaymentDetails(payments[i], result[i]);
            }

            return result;
        }

        public async Task<PaymentDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid payment id.");

            var payment = await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Invoice)
                .Include(x => x.Customer)
                .Include(x => x.SalesRep)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (payment == null)
                throw new KeyNotFoundException(
                    $"Payment with id {id} not found.");

            var dto = _mapper.Map<PaymentDto>(payment);

            PopulatePaymentDetails(payment, dto);

            return dto;
        }
        public async Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(
     int invoiceId)
        {
            if (invoiceId <= 0)
                throw new ArgumentException("Invalid invoice id.");

            var payments = await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Invoice)
                .Include(x => x.Customer)
                .Include(x => x.SalesRep)
                .Where(x =>
                    x.InvoiceId == invoiceId &&
                    x.IsActive)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            var result =
                _mapper.Map<List<PaymentDto>>(payments);

            for (var i = 0; i < payments.Count; i++)
            {
                PopulatePaymentDetails(
                    payments[i],
                    result[i]);
            }

            return result;
        }
        public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
        {
            var validationResult = await _createPaymentValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var invoice = await _unitOfWork.Invoices.GetByIdAsync(dto.InvoiceId)
                ?? throw new KeyNotFoundException($"Invoice with id {dto.InvoiceId} not found.");

            if (invoice.Status != InvoiceStatus.Confirmed)
                throw new InvalidOperationException("Payment is allowed only for confirmed invoices.");

            if (invoice.PaymentStatus == PaymentStatus.Paid)
                throw new InvalidOperationException("Invoice is already fully paid.");

            var remaining = invoice.TotalAmount - invoice.PaidAmount;

            if (remaining <= 0)
                throw new InvalidOperationException("Invoice has no remaining balance.");

            if (dto.Amount > remaining)
                throw new InvalidOperationException($"Payment amount exceeds remaining balance of {remaining}.");

            if (!invoice.SalesRepId.HasValue)
                throw new InvalidOperationException("Invoice is not assigned to a sales rep.");

            if (!invoice.SalesRepSessionId.HasValue)
                throw new InvalidOperationException("Invoice is not linked to a sales rep session.");

            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedAccessException("Current user is not authenticated.");

            var isSalesRepUser = string.Equals(_currentUser.Role, "SalesRep", StringComparison.OrdinalIgnoreCase);

            if (isSalesRepUser)
            {
                var salesRep = (await _unitOfWork.SalesReps
                    .FindAsync(s => s.UserId == _currentUser.UserId && s.IsActive))
                    .FirstOrDefault()
                    ?? throw new UnauthorizedAccessException("SalesRep not found for current user.");

                if (invoice.SalesRepId.Value != salesRep.Id)
                    throw new UnauthorizedAccessException("You cannot add payment to another sales rep's invoice.");

                var session = await _unitOfWork.SalesRepSessions.GetByIdAsync(invoice.SalesRepSessionId.Value)
                    ?? throw new KeyNotFoundException($"SalesRepSession with id {invoice.SalesRepSessionId.Value} not found.");

                if (session.SalesRepId != salesRep.Id)
                    throw new UnauthorizedAccessException("You cannot add payment to another sales rep's session.");
            }
            else
            {
                if (!dto.SalesRepId.HasValue)
                    throw new InvalidOperationException("SalesRepId is required for admin or supervisor payment creation.");

                if (!dto.SalesRepSessionId.HasValue)
                    throw new InvalidOperationException("SalesRepSessionId is required for admin or supervisor payment creation.");

                if (dto.SalesRepId.Value != invoice.SalesRepId.Value)
                    throw new InvalidOperationException("SalesRepId does not match invoice sales rep.");

                if (dto.SalesRepSessionId.Value != invoice.SalesRepSessionId.Value)
                    throw new InvalidOperationException("SalesRepSessionId does not match invoice sales rep session.");
            }

            var payment = _mapper.Map<Payment>(dto);

            payment.PaymentNumber = GeneratePaymentNumber();
            payment.CustomerId = invoice.CustomerId;
            payment.SalesRepId = invoice.SalesRepId;
            payment.SalesRepSessionId = invoice.SalesRepSessionId;
            payment.Status = PaymentStatus.Paid;
            payment.PaymentDate = DateTime.UtcNow;
            payment.CreatedAt = DateTime.UtcNow;
            payment.IsActive = true;

            // Update Invoice PaidAmount
            invoice.PaidAmount += dto.Amount;
            invoice.PaymentStatus =
                invoice.PaidAmount >= invoice.TotalAmount
                ? PaymentStatus.Paid
                : PaymentStatus.PartiallyPaid;

            invoice.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Payments.AddAsync(payment);
            _unitOfWork.Invoices.Update(invoice);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<PaymentDto>(payment);
        }


        #region Helper Method

        private static string GeneratePaymentNumber() => $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        private static void PopulatePaymentDetails(
    Payment payment,
    PaymentDto dto)
        {
            if (payment.Invoice is not null)
            {
                dto.InvoiceNumber =
                    payment.Invoice.InvoiceNumber;
            }

            if (payment.Customer is not null)
            {
                dto.CustomerName =
                    payment.Customer.Name;

                dto.CustomerPhone =
                    payment.Customer.Phone;
            }

            if (payment.SalesRep is not null)
            {
                dto.SalesRepCode =
                    payment.SalesRep.SalesRepCode;

                dto.SalesRepName =
                    payment.SalesRep.Name;
            }
        }
        #endregion

    }
}
