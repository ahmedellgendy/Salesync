using AutoMapper;
using Salesync.Application.Dtos.CustomerDto;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Domain.Entities;

namespace Salesync.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public CustomerService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllAsync()
        {
            var customers = await _unitOfWork.Customers.GetAllAsync();
            return _mapper.Map<IEnumerable<CustomerDto>>(customers);
        }

        public async Task<CustomerDto?> GetByIdAsync(int id)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            return _mapper.Map<CustomerDto?>(customer);
        }

        public async Task<CustomerDto> CreateAsync(CreateCustomerDto createCustomerDto)
        {
            // Check if a customer with the same email already exists
            var allCustomers = await _unitOfWork.Customers.GetAllAsync();
            if (allCustomers.Any(c => c.Email == createCustomerDto.Email))
            {
                throw new Exception($"A customer with the email '{createCustomerDto.Email}' already exists.");
            }

            // Map from Dto to Customer to save at db
            var customer = _mapper.Map<Customer>(createCustomerDto);
            await _unitOfWork.Customers.CreateAsync(customer);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto updateCustomerDto)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (customer == null)
                throw new Exception($"Customer with id '{id}' not found.");

            _mapper.Map(updateCustomerDto, customer);

            await _unitOfWork.CompleteAsync();

            return _mapper.Map<CustomerDto>(customer);
        }

        public async Task DeleteAsync(int id)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);
            if (customer == null)
                throw new Exception($"Customer with id '{id}' not found.");

            _unitOfWork.Customers.DeleteAsync(customer);
            await _unitOfWork.CompleteAsync();

        }



    }
}
