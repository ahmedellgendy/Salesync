using AutoMapper;
using FluentValidation;
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
            var customers =
                await _unitOfWork.Customers.GetAllAsync();

            var result =
                new List<CustomerDto>();

            foreach (var customer in customers)
            {
                var dto =
                    await MapCustomerAsync(customer);

                result.Add(dto);
            }

            return result;
        }

        public async Task<CustomerDto?> GetByIdAsync(int id)
        {
            var customer =
                await _unitOfWork.Customers.GetByIdAsync(id);

            if (customer is null)
                return null;

            return await MapCustomerAsync(customer);
        }

        public async Task<CustomerDto> CreateAsync(
            CreateCustomerDto createCustomerDto)
        {
            var validationResult =
                await _createValidator.ValidateAsync(
                    createCustomerDto);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    validationResult.Errors);
            }

            var allCustomers =
                await _unitOfWork.Customers.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(
                    createCustomerDto.Email))
            {
                var emailExists =
                    allCustomers.Any(c =>
                        !string.IsNullOrWhiteSpace(c.Email) &&
                        c.Email.Equals(
                            createCustomerDto.Email,
                            StringComparison.OrdinalIgnoreCase));

                if (emailExists)
                {
                    throw new ArgumentException(
                        $"A customer with the email '{createCustomerDto.Email}' already exists.");
                }
            }

            await ValidatePriceListAsync(
                createCustomerDto.PriceListId);

            var customer =
                _mapper.Map<Customer>(
                    createCustomerDto);

            customer.PriceListId =
                createCustomerDto.PriceListId;

            await _unitOfWork.Customers
                .AddAsync(customer);

            await _unitOfWork.CompleteAsync();

            return await MapCustomerAsync(customer);
        }

        public async Task<CustomerDto> UpdateAsync(
            int id,
            UpdateCustomerDto updateCustomerDto)
        {
            var validationResult =
                await _updateValidator.ValidateAsync(
                    updateCustomerDto);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    validationResult.Errors);
            }

            var customer =
                await _unitOfWork.Customers.GetByIdAsync(id);

            if (customer is null)
            {
                throw new KeyNotFoundException(
                    $"Customer with id '{id}' not found.");
            }

            var allCustomers =
                await _unitOfWork.Customers.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(
                    updateCustomerDto.Email))
            {
                var emailExists =
                    allCustomers.Any(c =>
                        c.Id != id &&
                        !string.IsNullOrWhiteSpace(c.Email) &&
                        c.Email.Equals(
                            updateCustomerDto.Email,
                            StringComparison.OrdinalIgnoreCase));

                if (emailExists)
                {
                    throw new ArgumentException(
                        $"A customer with the email '{updateCustomerDto.Email}' already exists.");
                }
            }

            await ValidatePriceListAsync(
                updateCustomerDto.PriceListId);

            _mapper.Map(
                updateCustomerDto,
                customer);

            customer.PriceListId =
                updateCustomerDto.PriceListId;

            await _unitOfWork.CompleteAsync();

            return await MapCustomerAsync(customer);
        }

        public async Task DeleteAsync(int id)
        {
            var customer =
                await _unitOfWork.Customers.GetByIdAsync(id);

            if (customer is null)
            {
                throw new KeyNotFoundException(
                    $"Customer with id '{id}' not found.");
            }

            _unitOfWork.Customers.Delete(customer);

            await _unitOfWork.CompleteAsync();
        }

        private async Task ValidatePriceListAsync(
            int? priceListId)
        {
            if (!priceListId.HasValue)
                return;

            var priceList =
                await _unitOfWork.PriceLists
                    .GetByIdAsync(priceListId.Value);

            if (priceList is null)
            {
                throw new ArgumentException(
                    $"Price list with id '{priceListId.Value}' was not found.");
            }

            if (!priceList.IsActive)
            {
                throw new ArgumentException(
                    $"Price list with id '{priceListId.Value}' is inactive.");
            }
        }

        private async Task<CustomerDto> MapCustomerAsync(
            Customer customer)
        {
            var dto =
                _mapper.Map<CustomerDto>(customer);

            dto.PriceListId =
                customer.PriceListId;

            if (customer.PriceListId.HasValue)
            {
                var priceList =
                    await _unitOfWork.PriceLists
                        .GetByIdAsync(
                            customer.PriceListId.Value);

                if (priceList is not null)
                {
                    dto.PriceListCode =
                        priceList.Code;

                    dto.PriceListName =
                        priceList.Name;
                }
            }

            return dto;
        }
    }
}