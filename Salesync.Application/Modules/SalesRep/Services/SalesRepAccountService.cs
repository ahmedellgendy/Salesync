using AutoMapper;
using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Identity.Dtos.User;
using Salesync.Application.Modules.Identity.Interfaces;
using Salesync.Application.Modules.SalesRep.Dtos.SalesRepAccount;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using SalesRepEntity =Salesync.Domain.Modules.SalesRep.Entities.SalesRep;
using SalesRepResponseDto =Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto.SalesRepDto;

namespace Salesync.Application.Modules.SalesRep.Services
{
    public sealed class SalesRepAccountService: ISalesRepAccountService
    {
        private const string SalesRepRole = "SalesRep";
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateSalesRepWithAccountDto> _validator;

        public SalesRepAccountService(
            IUnitOfWork unitOfWork,
            IUserService userService,
            IMapper mapper,
            IValidator<CreateSalesRepWithAccountDto> validator)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<CreatedSalesRepWithAccountDto> CreateAsync(CreateSalesRepWithAccountDto dto)
        {
            var validationResult =
                await _validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    validationResult.Errors);
            }

            await ValidateBusinessRulesAsync(dto);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var createdUser =await _userService.CreateSalesRepUserAsync(
                        new CreateSalesRepUserDto
                        {
                            UserName =
                                dto.Account.UserName,

                            Password =
                                dto.Account.TemporaryPassword,

                            FullName =
                                dto.SalesRep.Name,

                            PhoneNumber =
                                dto.SalesRep.Phone,

                            Email =
                                dto.SalesRep.Email,

                            BranchId =
                                dto.SalesRep.BranchId,

                            BusinessUnitId =
                                dto.SalesRep.BusinessUnitId
                        });

                var salesRep =_mapper.Map<SalesRepEntity>(dto.SalesRep);

                salesRep.UserId = createdUser.Id;
                salesRep.IsActive = true;
                salesRep.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.SalesReps
                    .AddAsync(salesRep);

                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new CreatedSalesRepWithAccountDto
                {
                    SalesRep =
                        _mapper.Map<SalesRepResponseDto>(
                            salesRep),

                    UserId = createdUser.Id,
                    UserName = createdUser.UserName,
                    Role = SalesRepRole
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task ValidateBusinessRulesAsync(CreateSalesRepWithAccountDto dto)
        {
            var salesRepCode =
                dto.SalesRep.SalesRepCode.Trim();

            var salesRepCodeExists =
                await _unitOfWork.SalesReps.ExistsAsync(
                    salesRep =>
                        salesRep.SalesRepCode == salesRepCode);

            if (salesRepCodeExists)
            {
                throw new InvalidOperationException(
                    $"SalesRep with code '{salesRepCode}' already exists.");
            }

            var branchExists =
                await _unitOfWork.Branches.ExistsAsync(
                    branch =>
                        branch.Id == dto.SalesRep.BranchId &&
                        branch.IsActive);

            if (!branchExists)
            {
                throw new KeyNotFoundException(
                    $"Active branch with id " +
                    $"'{dto.SalesRep.BranchId}' was not found.");
            }

            if (!dto.SalesRep.SupervisorId.HasValue)
            {
                return;
            }

            var supervisorExists =
                await _unitOfWork.SalesReps.ExistsAsync(
                    salesRep =>
                        salesRep.Id ==
                            dto.SalesRep.SupervisorId.Value &&
                        salesRep.IsActive);

            if (!supervisorExists)
            {
                throw new KeyNotFoundException(
                    $"Active supervisor with id " +
                    $"'{dto.SalesRep.SupervisorId.Value}' " +
                    "was not found.");
            }
        }
    }
}