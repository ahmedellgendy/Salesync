using AutoMapper;
using Salesync.Application.Modules.MasterData.Dtos.BranchDto;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;
using Salesync.Application.Modules.MasterData.Dtos.ProductDto;
using Salesync.Application.Modules.MasterData.Dtos.WarehouseDto;
using Salesync.Application.Modules.Sales.Dtos.Invoice;
using Salesync.Application.Modules.Sales.Dtos.InvoiceItem;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.SalesRep.Dtos.RouteCustomerDto;
using Salesync.Application.Modules.SalesRep.Dtos.RouteDto;
using Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;


namespace Salesync.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region MasterData

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

            #endregion

            #region SalesRep

            CreateMap<SalesRep, SalesRepDto>();
            CreateMap<CreateSalesRepDto, SalesRep>();
            CreateMap<UpdateSalesRepDto, SalesRep>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Route, RouteDto>();
            CreateMap<CreateRouteDto, Route>();
            CreateMap<UpdateRouteDto, Route>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<RouteCustomer, RouteCustomerDto>();
            CreateMap<CreateRouteCustomerDto, RouteCustomer>();
            CreateMap<UpdateRouteCustomerDto, RouteCustomer>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            #endregion

            #region Sales

            CreateMap<SalesRepSession, SalesRepSessionDto>();
            CreateMap<CreateSalesRepSessionDto, SalesRepSession>();

            CreateMap<Invoice, InvoiceDto>();
            CreateMap<CreateInvoiceDto, Invoice>();
            CreateMap<UpdateInvoiceDto, Invoice>()
                .ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<InvoiceItem, InvoiceItemDto>();
            CreateMap<CreateInvoiceItemDto, InvoiceItem>();

            CreateMap<Payment, PaymentDto>();
            CreateMap<CreatePaymentDto, Payment>();

            CreateMap<InvoiceReturn, InvoiceReturnDto>();
            CreateMap<CreateInvoiceReturnDto, InvoiceReturn>();

            CreateMap<CreateInvoiceReturnItemDto, InvoiceReturnItem>();

            #endregion
        }
    }
}
