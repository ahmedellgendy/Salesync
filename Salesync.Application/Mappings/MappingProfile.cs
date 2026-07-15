using AutoMapper;
using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.Inventory.Dtos;
using Salesync.Application.Modules.LoadRequest.Dtos;
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
using Salesync.Application.Modules.Treasury.Dtos;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Domain.Modules.CustomerVisit.Entities;
using Salesync.Domain.Modules.Inventory.Entities;
using Salesync.Domain.Modules.LoadRequest.Entities;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Salesync.Domain.Modules.Treasury.Entities;
using ClosingEntity = Salesync.Domain.Modules.Sales.Entities.SalesRepDayClosing;
using ClosingItemEntity = Salesync.Domain.Modules.Sales.Entities.SalesRepDayClosingItem;


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

            CreateMap<Invoice, InvoiceDto>()
                .ForMember(dest => dest.RemainingAmount, opt => opt.MapFrom(src => src.TotalAmount - src.PaidAmount));
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

            CreateMap<ClosingEntity, SalesRepDayClosingDto>()
                .ForMember(dest => dest.SalesRepName, opt => opt.MapFrom(src => src.SalesRep.Name))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<ClosingItemEntity, SalesRepDayClosingItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.ItemCode, opt => opt.MapFrom(src => src.Product.ItemCode));

            #endregion

            #region Inventory

            CreateMap<StockBalance, StockBalanceDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name));

            CreateMap<StockMovement, StockMovementDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name));

            #endregion

            #region LoadRequest

            CreateMap<LoadRequest, LoadRequestDto>()
                .ForMember(dest => dest.SalesRepName, opt => opt.MapFrom(src => src.SalesRep.Name))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<LoadRequestItem, LoadRequestItemDto>();

            CreateMap<SalesRepInventory, SalesRepInventoryDto>()
                .ForMember(dest => dest.SalesRepName, opt => opt.MapFrom(src => src.SalesRep.Name))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.ItemCode, opt => opt.MapFrom(src => src.Product.ItemCode));

            CreateMap<SalesRepInventoryMovement, SalesRepInventoryMovementDto>()
                .ForMember(dest => dest.SalesRepName, opt => opt.MapFrom(src => src.SalesRep.Name))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.ItemCode, opt => opt.MapFrom(src => src.Product.ItemCode));

            #endregion

            #region CustomerVisit

            CreateMap<CustomerVisit, CustomerVisitDto>()
                .ForMember(dest => dest.SalesRepName, opt => opt.MapFrom(src => src.SalesRep.Name))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
                .ForMember(dest => dest.RouteName, opt => opt.MapFrom(src => src.Route != null ? src.Route.Name : null));

            CreateMap<StartCustomerVisitDto, CustomerVisit>();

            CreateMap<CustomerVisit, CustomerVisitDto>()
                .ForMember(dest => dest.SalesRepName, opt => opt.MapFrom(src => src.SalesRep.Name))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
                .ForMember(dest => dest.RouteName, opt => opt.MapFrom(src => src.Route != null ? src.Route.Name : null));

            #endregion

            #region Treasury

            CreateMap<CashBox, CashBoxDto>();

            CreateMap<CashReceipt, CashReceiptDto>();

            CreateMap<SalesRepCashLedger, SalesRepCashLedgerDto>();

            #endregion
        }
    }
}
