using AutoMapper;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Sales.Dtos.Invoice;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Application.Modules.Sales.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InvoiceService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
        {
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            return _mapper.Map<IEnumerable<InvoiceDto>>(invoices);
        }
        public async Task<InvoiceDto> GetByIdAsync(int id)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with id {id} not found.");
            return _mapper.Map<InvoiceDto>(invoice);
        }
        public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
        {
            var invoice = _mapper.Map<Invoice>(dto);
            invoice.InvoiceNumber = GenerateInvoiceNumber();
            invoice.Status = InvoiceStatus.Draft;
            invoice.PaymentStatus = PaymentStatus.Pending;

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<InvoiceDto>(invoice);
        }

        public async Task<InvoiceDto> UpdateAsync(int id, UpdateInvoiceDto dto)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with id {id} not found.");

            _mapper.Map(dto, invoice);
            invoice.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Invoices.Update(invoice);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<InvoiceDto>(invoice);
        }
        public async Task CancelAsync(int id)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with id {id} not found.");

            if (invoice.Status == InvoiceStatus.Confirmed)
                throw new InvalidOperationException("Cannot cancel a confirmed invoice.");

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Invoices.Update(invoice);
            await _unitOfWork.CompleteAsync();
        }

        #region Helper Method

        private static string GenerateInvoiceNumber() => $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        #endregion

    }
}
