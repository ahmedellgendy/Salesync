using AutoMapper;
using Salesync.Application.Dtos.BranchDto;
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
            CreateMap<CreateWarehouseDto, Warehouse>();
            CreateMap<UpdateWarehouseDto, Warehouse>();
        }
    }
}
