using AutoMapper;
using Salesync.Application.Dtos;
using Salesync.Domain.Entities;


namespace Salesync.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Branch, BranchDto>().ReverseMap();
        }
    }
}
