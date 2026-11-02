using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Common.Exceptions;
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
        private readonly ISalesAccessScopeService _access;
        private readonly ICurrentUserService _actor;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateCustomerDto> _createValidator;
        private readonly IValidator<UpdateCustomerDto> _updateValidator;

        public CustomerService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateCustomerDto> createValidator,
            IValidator<UpdateCustomerDto> updateValidator, ISalesAccessScopeService access, ICurrentUserService actor)
        {
            _unitOfWork = unitOfWork;
            _access = access;
            _actor = actor;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllAsync()
        {
            var customers =
                await (await VisibleCustomersAsync()).AsNoTracking().ToListAsync();

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
                await (await VisibleCustomersAsync()).FirstOrDefaultAsync(x => x.Id == id);

            if (customer is null)
                return null;

            return await MapCustomerAsync(customer);
        }

        public async Task<CustomerDto> CreateAsync(
            CreateCustomerDto createCustomerDto)
        {
            await RequireCustomerWriterAsync();
            createCustomerDto.BranchId = await WriteBranchAsync(createCustomerDto.BranchId);
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
            await RequireCustomerWriterAsync();
            var validationResult =
                await _updateValidator.ValidateAsync(
                    updateCustomerDto);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    validationResult.Errors);
            }

            var customer =
                await (await VisibleCustomersAsync()).FirstOrDefaultAsync(x => x.Id == id);

            if (customer is null)
            {
                throw new KeyNotFoundException(
                    $"Customer with id '{id}' not found.");
            }

            if (_actor.Role == "Supervisor")
                updateCustomerDto.BranchId = await WriteBranchAsync(updateCustomerDto.BranchId ?? customer.BranchId);

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
            if (_actor.Role != "Admin") throw new ForbiddenException("Only administration can delete customers.");
            var customer =
                await (await VisibleCustomersAsync()).FirstOrDefaultAsync(x => x.Id == id);

            if (customer is null)
            {
                throw new KeyNotFoundException(
                    $"Customer with id '{id}' not found.");
            }

            _unitOfWork.Customers.Delete(customer);

            await _unitOfWork.CompleteAsync();
        }

        private async Task<IQueryable<Customer>> VisibleCustomersAsync()
        {
            var scope = await _access.GetAsync();
            var query = _unitOfWork.Customers.GetQueryable();
            if (scope.AllowsAll) return query;
            var assigned = _unitOfWork.RouteCustomers.GetQueryable().Where(rc => rc.IsActive && rc.Route.IsActive &&
                ((rc.Route.AssignedSalesRepId.HasValue && scope.SalesRepIds.Contains(rc.Route.AssignedSalesRepId.Value)) ||
                 rc.Route.SalesRepRoutes.Any(a => a.IsActive && a.IsCurrent && !a.UnassignedDate.HasValue && scope.SalesRepIds.Contains(a.SalesRepId))));
            // Mobile representatives see their assigned routes; branch staff retain branch master-data access.
            return query.Where(c => assigned.Any(rc => rc.CustomerId == c.Id) ||
                (_actor.Role != "SalesRep" && c.BranchId.HasValue && scope.BranchIds.Contains(c.BranchId.Value)));
        }

        private async Task RequireCustomerWriterAsync()
        {
            await _access.GetAsync();
            if (_actor.Role is not ("Admin" or "Supervisor")) throw new ForbiddenException("Customer maintenance requires administration or supervision.");
        }

        private async Task<int?> WriteBranchAsync(int? branchId)
        {
            var scope = await _access.GetAsync();
            if (scope.AllowsAll) return branchId;
            branchId ??= scope.BranchIds.Length == 1 ? scope.BranchIds[0] : null;
            if (!branchId.HasValue || !scope.BranchIds.Contains(branchId.Value))
                throw new ForbiddenException("Customer branch is outside your authorized scope.");
            return branchId;
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