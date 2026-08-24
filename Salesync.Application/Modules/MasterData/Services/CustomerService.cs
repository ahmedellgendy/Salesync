using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;
using Salesync.Application.Modules.MasterData.Interfaces.Services;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Application.Modules.MasterData.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateCustomerDto> _createValidator;
        private readonly IValidator<UpdateCustomerDto> _updateValidator;

        public CustomerService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateCustomerDto> createValidator,
            IValidator<UpdateCustomerDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllAsync()
        {
            var customers = await _unitOfWork.Customers
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Branch)
                .Include(x => x.HeadOffice)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerDto>>(customers);
        }

        public async Task<CustomerDto?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid customer id.");

            var customer = await _unitOfWork.Customers
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Branch)
                .Include(x => x.HeadOffice)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            return customer == null
                ? null
                : _mapper.Map<CustomerDto>(customer);
        }

        public async Task<CustomerDto> CreateAsync(
            CreateCustomerDto dto)
        {
            var validationResult =
                await _createValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(
                    validationResult.Errors);

            NormalizeCreateDto(dto);

            await ValidateEmailAsync(dto.Email);

            Branch? branch = null;

            if (dto.BranchId.HasValue)
            {
                branch = await _unitOfWork.Branches
                    .GetByIdAsync(dto.BranchId.Value);

                if (branch == null)
                {
                    throw new KeyNotFoundException(
                        $"Branch with ID {dto.BranchId.Value} does not exist.");
                }
            }

            Customer? headOffice = null;

            if (dto.HeadOfficeId.HasValue)
            {
                headOffice = await _unitOfWork.Customers
                    .GetQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.Id == dto.HeadOfficeId.Value &&
                        x.IsActive);

                if (headOffice == null)
                {
                    throw new KeyNotFoundException(
                        $"Head office customer with ID {dto.HeadOfficeId.Value} does not exist.");
                }

                if (!headOffice.IsHeadOffice)
                {
                    throw new InvalidOperationException(
                        $"Customer '{headOffice.Name}' is not configured as a head office.");
                }
            }

            if (dto.IsHeadOffice &&
                dto.HeadOfficeId.HasValue)
            {
                throw new InvalidOperationException(
                    "A head office customer cannot belong to another head office.");
            }

            var customer =
                _mapper.Map<Customer>(dto);

            customer.Branch = branch;
            customer.HeadOffice = headOffice;

            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task<CustomerDto> UpdateAsync(
            int id,
            UpdateCustomerDto dto)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid customer id.");

            var validationResult =
                await _updateValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(
                    validationResult.Errors);

            var customer = await _unitOfWork.Customers
                .GetQueryable()
                .Include(x => x.Branch)
                .Include(x => x.HeadOffice)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (customer == null)
            {
                throw new KeyNotFoundException(
                    $"Customer with ID {id} not found.");
            }

            NormalizeUpdateDto(dto);

            await ValidateEmailAsync(
                dto.Email,
                id);

            Branch? branch = null;

            if (dto.BranchId.HasValue)
            {
                branch = await _unitOfWork.Branches
                    .GetByIdAsync(dto.BranchId.Value);

                if (branch == null)
                {
                    throw new KeyNotFoundException(
                        $"Branch with ID {dto.BranchId.Value} does not exist.");
                }
            }

            Customer? headOffice = null;

            if (dto.HeadOfficeId.HasValue)
            {
                if (dto.HeadOfficeId.Value == id)
                {
                    throw new InvalidOperationException(
                        "A customer cannot be its own head office.");
                }

                headOffice = await _unitOfWork.Customers
                    .GetQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.Id == dto.HeadOfficeId.Value &&
                        x.IsActive);

                if (headOffice == null)
                {
                    throw new KeyNotFoundException(
                        $"Head office customer with ID {dto.HeadOfficeId.Value} does not exist.");
                }

                if (!headOffice.IsHeadOffice)
                {
                    throw new InvalidOperationException(
                        $"Customer '{headOffice.Name}' is not configured as a head office.");
                }
            }

            var finalIsHeadOffice =
                dto.IsHeadOffice ??
                customer.IsHeadOffice;

            var finalHeadOfficeId =
                dto.HeadOfficeId ??
                customer.HeadOfficeId;

            if (finalIsHeadOffice &&
                finalHeadOfficeId.HasValue)
            {
                throw new InvalidOperationException(
                    "A head office customer cannot belong to another head office.");
            }

            _mapper.Map(dto, customer);

            if (branch != null)
                customer.Branch = branch;

            if (headOffice != null)
                customer.HeadOffice = headOffice;

            customer.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Customers.Update(customer);

            await _unitOfWork.CompleteAsync();

            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid customer id.");

            var customer = await _unitOfWork.Customers
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (customer == null)
            {
                throw new KeyNotFoundException(
                    $"Customer with ID {id} not found.");
            }

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Customers.Update(customer);

            await _unitOfWork.CompleteAsync();
        }

        private async Task ValidateEmailAsync(
            string? email,
            int? excludedCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            var exists = await _unitOfWork.Customers
                .GetQueryable()
                .AnyAsync(x =>
                    x.Email == email &&
                    (!excludedCustomerId.HasValue ||
                     x.Id != excludedCustomerId.Value));

            if (exists)
            {
                throw new InvalidOperationException(
                    $"A customer with the email '{email}' already exists.");
            }
        }

        private static void NormalizeCreateDto(
            CreateCustomerDto dto)
        {
            dto.Name = dto.Name.Trim();

            dto.Phone = NormalizeOptional(dto.Phone);
            dto.Email = NormalizeOptional(dto.Email)?.ToLowerInvariant();

            dto.Country = NormalizeOptional(dto.Country);
            dto.Address = NormalizeOptional(dto.Address);
            dto.Area = NormalizeOptional(dto.Area);
            dto.City = NormalizeOptional(dto.City);
            dto.District = NormalizeOptional(dto.District);
            dto.Region = NormalizeOptional(dto.Region);
            dto.PostalCode = NormalizeOptional(dto.PostalCode);

            dto.CategoryCode = NormalizeOptional(dto.CategoryCode);
            dto.SalesSectorCode = NormalizeOptional(dto.SalesSectorCode);
            dto.ClassId = NormalizeOptional(dto.ClassId);

            dto.PaymentTermsCode =
                NormalizeOptional(dto.PaymentTermsCode);

            dto.AccountNumber =
                NormalizeOptional(dto.AccountNumber);

            dto.TaxId =
                NormalizeOptional(dto.TaxId);

            dto.PriceId =
                NormalizeOptional(dto.PriceId);
        }

        private static void NormalizeUpdateDto(
            UpdateCustomerDto dto)
        {
            dto.Name = NormalizeOptional(dto.Name);
            dto.Phone = NormalizeOptional(dto.Phone);

            dto.Email =
                NormalizeOptional(dto.Email)?
                    .ToLowerInvariant();

            dto.Country = NormalizeOptional(dto.Country);
            dto.Address = NormalizeOptional(dto.Address);
            dto.Area = NormalizeOptional(dto.Area);
            dto.City = NormalizeOptional(dto.City);
            dto.District = NormalizeOptional(dto.District);
            dto.Region = NormalizeOptional(dto.Region);
            dto.PostalCode = NormalizeOptional(dto.PostalCode);

            dto.CategoryCode =
                NormalizeOptional(dto.CategoryCode);

            dto.SalesSectorCode =
                NormalizeOptional(dto.SalesSectorCode);

            dto.ClassId =
                NormalizeOptional(dto.ClassId);

            dto.PaymentTermsCode =
                NormalizeOptional(dto.PaymentTermsCode);

            dto.AccountNumber =
                NormalizeOptional(dto.AccountNumber);

            dto.TaxId =
                NormalizeOptional(dto.TaxId);

            dto.PriceId =
                NormalizeOptional(dto.PriceId);
        }

        private static string? NormalizeOptional(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}