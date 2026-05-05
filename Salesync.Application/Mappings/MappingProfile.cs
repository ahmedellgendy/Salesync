using AutoMapper;
using Salesync.Application.Dtos.BranchDto;
using Salesync.Application.Dtos.CustomerDto;
using Salesync.Application.Dtos.ProductDto;
using Salesync.Application.Dtos.WarehouseDto;
using Salesync.Domain.Entities;


namespace Salesync.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Branch, BranchDto>().ReverseMap();

            CreateMap<Warehouse, WarehouseDto>();
            CreateMap<CreateWarehouseDto, Warehouse>().ReverseMap();
            CreateMap<UpdateWarehouseDto, Warehouse>();

            CreateMap<Product, ProductDto>().ReverseMap();
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));


            CreateMap<Customer, CustomerDto>().ReverseMap();
            CreateMap<CreateCustomerDto, Customer>().ReverseMap();
            CreateMap<UpdateCustomerDto, Customer>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

        }
    }
}
