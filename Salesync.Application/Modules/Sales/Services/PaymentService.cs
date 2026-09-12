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
        public async Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId)
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

        public async Task<IEnumerable<OutstandingInvoiceDto>> GetCurrentSalesRepOutstandingInvoicesAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedAccessException(
                    "Current user is not authenticated.");
            }


            var salesRep =
                (await _unitOfWork.SalesReps
                    .FindAsync(x =>
                        x.UserId == _currentUser.UserId &&
                        x.IsActive))
                .FirstOrDefault()
                ?? throw new UnauthorizedAccessException(
                    "Sales rep not found for current user.");


            var invoices =
                await _unitOfWork.Invoices
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(x => x.Customer)
                    .Where(x =>
                        x.IsActive &&
                        x.Status == InvoiceStatus.Confirmed &&
                        x.SalesRepId == salesRep.Id &&
                        x.PaidAmount < x.TotalAmount)
                    .OrderByDescending(x =>
                        x.CreatedAt)
                    .Select(x =>
                        new OutstandingInvoiceDto
                        {
                            InvoiceId =
                                x.Id,

                            InvoiceNumber =
                                x.InvoiceNumber,

                            CustomerId =
                                x.CustomerId,

                            CustomerName =
                                x.Customer.Name,

                            InvoiceDate =
                                x.CreatedAt,

                            TotalAmount =
                                x.TotalAmount,

                            PaidAmount =
                                x.PaidAmount,

                            OutstandingAmount =
                                x.TotalAmount -
                                x.PaidAmount,

                            OriginalSalesRepSessionId =
                                x.SalesRepSessionId ?? 0
                        })
                    .ToListAsync();


            return invoices;
        }

        public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
        {
            var validationResult =
                await _createPaymentValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);


            var invoice =
                await _unitOfWork.Invoices
                    .GetByIdAsync(dto.InvoiceId)
                ?? throw new KeyNotFoundException(
                    $"Invoice with id {dto.InvoiceId} not found.");


            if (invoice.Status != InvoiceStatus.Confirmed)
            {
                throw new InvalidOperationException(
                    "Payment is allowed only for confirmed invoices.");
            }


            if (invoice.PaymentStatus == PaymentStatus.Paid)
            {
                throw new InvalidOperationException(
                    "Invoice is already fully paid.");
            }


            var remaining =
                invoice.TotalAmount -
                invoice.PaidAmount;


            if (remaining <= 0)
            {
                throw new InvalidOperationException(
                    "Invoice has no remaining balance.");
            }


            if (dto.Amount <= 0)
            {
                throw new InvalidOperationException(
                    "Payment amount must be greater than zero.");
            }


            if (dto.Amount > remaining)
            {
                throw new InvalidOperationException(
                    $"Payment amount exceeds remaining balance of {remaining:N2}.");
            }


            var invoiceSalesRepId =
                invoice.SalesRepId
                ?? throw new InvalidOperationException(
                    "Invoice is not assigned to a sales rep.");


            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedAccessException(
                    "Current user is not authenticated.");
            }


            var isSalesRepUser =
                string.Equals(
                    _currentUser.Role,
                    "SalesRep",
                    StringComparison.OrdinalIgnoreCase);


            int paymentSessionId;


            // =========================================================
            // Sales Rep
            // =========================================================

            if (isSalesRepUser)
            {
                var salesRep =
                    (await _unitOfWork.SalesReps
                        .FindAsync(x =>
                            x.UserId == _currentUser.UserId &&
                            x.IsActive))
                    .FirstOrDefault()
                    ?? throw new UnauthorizedAccessException(
                        "Sales rep not found for current user.");


                if (salesRep.Id != invoiceSalesRepId)
                {
                    throw new UnauthorizedAccessException(
                        "You cannot collect payment for another sales rep's invoice.");
                }


                /*
                 * Important:
                 *
                 * The invoice can belong to an OLD session.
                 *
                 * The new payment must belong to the CURRENT
                 * active session so it is included in today's
                 * collections and day closing.
                 */

                var activeSession =
                    await _unitOfWork.SalesRepSessions
                        .GetQueryable()
                        .Where(x =>
                            x.SalesRepId == salesRep.Id &&
                            x.IsActive &&
                            !x.EndTime.HasValue &&
                            !x.IsTreasurySettled)
                        .OrderByDescending(x =>
                            x.StartTime)
                        .FirstOrDefaultAsync();


                if (activeSession == null)
                {
                    throw new InvalidOperationException(
                        "No active sales rep session was found.");
                }


                if (activeSession.IsStockSettled)
                {
                    throw new InvalidOperationException(
                        "Cannot collect payment after the current session stock has been settled.");
                }


                paymentSessionId =
                    activeSession.Id;
            }

            // =========================================================
            // Admin / Supervisor
            // =========================================================

            else
            {
                if (!dto.SalesRepId.HasValue)
                {
                    throw new InvalidOperationException(
                        "SalesRepId is required for admin or supervisor payment creation.");
                }


                if (dto.SalesRepId.Value != invoiceSalesRepId)
                {
                    throw new InvalidOperationException(
                        "SalesRepId does not match invoice sales rep.");
                }


                if (!dto.SalesRepSessionId.HasValue)
                {
                    throw new InvalidOperationException(
                        "SalesRepSessionId is required for admin or supervisor payment creation.");
                }


                var paymentSession =
                    await _unitOfWork.SalesRepSessions
                        .GetQueryable()
                        .FirstOrDefaultAsync(x =>
                            x.Id == dto.SalesRepSessionId.Value &&
                            x.SalesRepId == invoiceSalesRepId &&
                            x.IsActive);


                if (paymentSession == null)
                {
                    throw new InvalidOperationException(
                        "Sales rep session was not found.");
                }


                if (paymentSession.EndTime.HasValue)
                {
                    throw new InvalidOperationException(
                        "Cannot add payment to a closed sales rep session.");
                }


                if (paymentSession.IsStockSettled)
                {
                    throw new InvalidOperationException(
                        "Cannot add payment after the sales rep session stock has been settled.");
                }


                if (paymentSession.IsTreasurySettled)
                {
                    throw new InvalidOperationException(
                        "Cannot add payment to a treasury-settled session.");
                }


                paymentSessionId =
                    paymentSession.Id;
            }


            // =========================================================
            // Create Payment
            // =========================================================

            var payment =
                _mapper.Map<Payment>(dto);


            payment.PaymentNumber =
                GeneratePaymentNumber();

            payment.InvoiceId =
                invoice.Id;

            payment.CustomerId =
                invoice.CustomerId;

            payment.SalesRepId =
                invoiceSalesRepId;

            /*
             * Payment is linked to CURRENT collection session,
             * not necessarily the original invoice session.
             */
            payment.SalesRepSessionId =
                paymentSessionId;

            payment.Status =
                PaymentStatus.Paid;

            payment.PaymentDate =
                DateTime.UtcNow;

            payment.CreatedAt =
                DateTime.UtcNow;

            payment.IsActive =
                true;


            // =========================================================
            // Update Invoice
            // =========================================================

            invoice.PaidAmount +=
                dto.Amount;


            invoice.PaymentStatus =
                invoice.PaidAmount >= invoice.TotalAmount
                    ? PaymentStatus.Paid
                    : PaymentStatus.PartiallyPaid;


            invoice.UpdatedAt =
                DateTime.UtcNow;


            // =========================================================
            // Save
            // =========================================================

            await _unitOfWork.Payments
                .AddAsync(payment);


            _unitOfWork.Invoices
                .Update(invoice);


            await _unitOfWork
                .CompleteAsync();


            return _mapper.Map<PaymentDto>(
                payment);
        }


        #region Helper Method

        private static string GeneratePaymentNumber() => $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        private static void PopulatePaymentDetails(Payment payment, PaymentDto dto)
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
