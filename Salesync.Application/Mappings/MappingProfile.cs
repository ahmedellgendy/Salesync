using AutoMapper;
using Salesync.Application.Modules.MasterData.Dtos.BranchDto;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;
using Salesync.Application.Modules.MasterData.Dtos.ProductDto;
using Salesync.Application.Modules.MasterData.Dtos.WarehouseDto;
using Salesync.Domain.Modules.MasterData.Entities;


namespace Salesync.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Branch, BranchDto>().ReverseMap();
            CreateMap<CreateBranchDto, Branch>();
            CreateMap<UpdateBranchDto, Branch>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Warehouse, WarehouseDto>();
            CreateMap<CreateWarehouseDto, Warehouse>().ReverseMap();
            CreateMap<UpdateWarehouseDto, Warehouse>().ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


            CreateMap<Product, ProductDto>().ReverseMap();
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));


            CreateMap<Customer, CustomerDto>().ReverseMap();
            CreateMap<CreateCustomerDto, Customer>().ReverseMap();
            CreateMap<UpdateCustomerDto, Customer>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

        }
    }
}
