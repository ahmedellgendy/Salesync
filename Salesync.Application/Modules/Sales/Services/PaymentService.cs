using AutoMapper;
using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
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

        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreatePaymentDto> createPaymentValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createPaymentValidator = createPaymentValidator;
        }

        public async Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId)
        {
            var payments = await _unitOfWork.Payments.FindAsync(p => p.InvoiceId == invoiceId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
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

            if (dto.SalesRepId.HasValue && invoice.SalesRepId.HasValue && dto.SalesRepId.Value != invoice.SalesRepId.Value)
                throw new InvalidOperationException("SalesRepId does not match invoice sales rep.");

            if (dto.SalesRepSessionId.HasValue && invoice.SalesRepSessionId.HasValue && dto.SalesRepSessionId.Value != invoice.SalesRepSessionId.Value)
                throw new InvalidOperationException("SalesRepSessionId does not match invoice sales rep session.");

            var payment = _mapper.Map<Payment>(dto);

            payment.PaymentNumber = GeneratePaymentNumber();
            payment.CustomerId = invoice.CustomerId;
            payment.SalesRepId = dto.SalesRepId ?? invoice.SalesRepId;
            payment.SalesRepSessionId = dto.SalesRepSessionId ?? invoice.SalesRepSessionId;
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

        #endregion

    }
}
