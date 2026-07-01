using AutoMapper;
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

        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId)
        {
            var payments = await _unitOfWork.Payments.FindAsync(p => p.InvoiceId == invoiceId);
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }
        public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(dto.InvoiceId)
                ?? throw new KeyNotFoundException($"Invoice with id {dto.InvoiceId} not found.");

            var payment = _mapper.Map<Payment>(dto);
            payment.PaymentNumber = GeneratePaymentNumber();
            payment.CustomerId = invoice.CustomerId;
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
