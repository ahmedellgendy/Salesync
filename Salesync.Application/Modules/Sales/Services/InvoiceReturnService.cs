using AutoMapper;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Application.Modules.Sales.Services
{
    public class InvoiceReturnService : IInvoiceReturnService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InvoiceReturnService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<InvoiceReturnDto>> GetByInvoiceIdAsync(int invoiceId)
        {
            var returns = await _unitOfWork.InvoiceReturns.FindAsync(r => r.InvoiceId == invoiceId);
            return _mapper.Map<IEnumerable<InvoiceReturnDto>>(returns);
        }
        public async Task<InvoiceReturnDto> CreateAsync(CreateInvoiceReturnDto dto)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(dto.InvoiceId)
                ?? throw new KeyNotFoundException($"Invoice with id {dto.InvoiceId} not found.");

            var invoiceReturn = _mapper.Map<InvoiceReturn>(dto);
            invoiceReturn.ReturnNumber = GenerateReturnNumber();
            invoiceReturn.CustomerId = invoice.CustomerId;
            invoiceReturn.Status = ReturnStatus.Pending;
            invoiceReturn.CreatedAt = DateTime.UtcNow;
            invoiceReturn.IsActive = true;

            foreach (var item in invoiceReturn.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId)
                    ?? throw new KeyNotFoundException($"Product with id {item.ProductId} not found.");

                item.ProductName = product.Name;
                item.ItemCode = product.ItemCode;
                item.UnitPrice = product.UnitPrice;
                item.TotalAmount = item.Quantity * product.UnitPrice;
            }

            invoiceReturn.TotalAmount = invoiceReturn.Items.Sum(i => i.TotalAmount);


            await _unitOfWork.InvoiceReturns.AddAsync(invoiceReturn);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<InvoiceReturnDto>(invoiceReturn);
        }

        public async Task<InvoiceReturnDto> ApproveAsync(int id)
        {
            var invoiceReturn = await _unitOfWork.InvoiceReturns.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Return with id {id} not found.");

            if (invoiceReturn.Status != ReturnStatus.Pending)
                throw new InvalidOperationException("Only pending returns can be approved.");


            invoiceReturn.Status = ReturnStatus.Approved;
            invoiceReturn.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.InvoiceReturns.Update(invoiceReturn);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<InvoiceReturnDto>(invoiceReturn);
        }
        public async Task<InvoiceReturnDto> RejectAsync(int id)
        {
            var invoiceReturn = await _unitOfWork.InvoiceReturns.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Return with id {id} not found.");

            if (invoiceReturn.Status != ReturnStatus.Pending)
                throw new InvalidOperationException("Only pending returns can be rejected.");

            invoiceReturn.Status = ReturnStatus.Rejected;
            invoiceReturn.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.InvoiceReturns.Update(invoiceReturn);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<InvoiceReturnDto>(invoiceReturn);
        }


        #region Helper Method

        private static string GenerateReturnNumber() => $"RET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        #endregion

    }
}
