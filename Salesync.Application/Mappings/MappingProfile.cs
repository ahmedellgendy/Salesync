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
using Salesync.Application.Modules.UnloadRequest.Dtos;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Domain.Modules.CustomerVisit.Entities;
using Salesync.Domain.Modules.Inventory.Entities;
using Salesync.Domain.Modules.LoadRequest.Entities;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Salesync.Domain.Modules.Treasury.Entities;
using Salesync.Domain.Modules.UnloadRequest.Entities;
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

            CreateMap<Warehouse, WarehouseDto>().ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null));
            CreateMap<CreateWarehouseDto, Warehouse>();
            CreateMap<UpdateWarehouseDto, Warehouse>().ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Product, ProductDto>().ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : null));
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Customer, CustomerDto>().ReverseMap();
            CreateMap<CreateCustomerDto, Customer>().ReverseMap();
            CreateMap<UpdateCustomerDto, Customer>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            #endregion

            #region SalesRep

            CreateMap<SalesRep, SalesRepDto>()
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null))
                .ForMember(dest => dest.SupervisorName, opt => opt.MapFrom(src => src.Supervisor != null ? src.Supervisor.Name : null));
            CreateMap<CreateSalesRepDto, SalesRep>();
            CreateMap<UpdateSalesRepDto, SalesRep>()
                .ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Route, RouteDto>()
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null))
                .ForMember(dest => dest.AssignedSalesRepName, opt => opt.MapFrom(src => src.AssignedSalesRep != null ? src.AssignedSalesRep.Name : null));
            CreateMap<CreateRouteDto, Route>();
            CreateMap<UpdateRouteDto, Route>()
                .ForMember(
                    dest => dest.BranchId,
                    opt =>
                    {
                        opt.PreCondition(src => src.BranchId.HasValue);
                        opt.MapFrom(src => src.BranchId!.Value);
                    })
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<RouteCustomer, RouteCustomerDto>();
            CreateMap<CreateRouteCustomerDto, RouteCustomer>();
            CreateMap<UpdateRouteCustomerDto, RouteCustomer>().ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            #endregion

            #region Sales

            CreateMap<SalesRepSession, SalesRepSessionDto>();
            CreateMap<CreateSalesRepSessionDto, SalesRepSession>();

            CreateMap<Invoice, InvoiceDto>()
              .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : null))
              .ForMember(dest => dest.CustomerCode, opt => opt.MapFrom(src => src.CustomerId.ToString()))
              .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Phone : null))
              .ForMember(dest => dest.CustomerAddress, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Address : null))
              .ForMember(dest => dest.SalesRepCode, opt => opt.MapFrom(src => src.SalesRep != null ? src.SalesRep.SalesRepCode : null))
              .ForMember(dest => dest.SalesRepName, opt => opt.MapFrom(src => src.SalesRep != null ? src.SalesRep.Name : null))
              .ForMember(dest => dest.SalesRepPhone, opt => opt.MapFrom(src => src.SalesRep != null ? src.SalesRep.Phone : null))
              .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.CreatedAt))
              .ForMember(dest => dest.ReturnsAmount, opt => opt.MapFrom(src => 0m))
              .ForMember(dest => dest.RemainingAmount, opt => opt.MapFrom(src => src.TotalAmount - src.PaidAmount));

            CreateMap<CreateInvoiceDto, Invoice>();
            CreateMap<UpdateInvoiceDto, Invoice>()
                .ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<InvoiceItem, InvoiceItemDto>()
                .ForMember(dest => dest.SmallUnit, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.SmallUnit) ? "قطعة" : src.SmallUnit))
                .ForMember(dest => dest.LargeUnit, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.LargeUnit) ? "كرتونة" : src.LargeUnit))
                .ForMember(dest => dest.UnitsPerLargeUnit, opt => opt.MapFrom(src => src.UnitsPerLargeUnit <= 0 ? 1 : src.UnitsPerLargeUnit));
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
                .ForMember(dest => dest.ItemCode, opt => opt.MapFrom(src => src.Product.ItemCode))
                .ForMember(dest => dest.SmallUnit, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Product.SmallUnit) ? "قطعة" : src.Product.SmallUnit))
                .ForMember(dest => dest.LargeUnit, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Product.LargeUnit) ? "كرتونة" : src.Product.LargeUnit))
                .ForMember(dest => dest.UnitsPerLargeUnit, opt => opt.MapFrom(src => src.Product.UnitsPerLargeUnit <= 0 ? 1 : src.Product.UnitsPerLargeUnit));

            CreateMap<SalesRepInventoryMovement, SalesRepInventoryMovementDto>()
                .ForMember(dest => dest.SalesRepName, opt => opt.MapFrom(src => src.SalesRep.Name))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.ItemCode, opt => opt.MapFrom(src => src.Product.ItemCode));

            #endregion

            #region CustomerVisit


            CreateMap<StartCustomerVisitDto, CustomerVisit>();

            CreateMap<CustomerVisit, CustomerVisitDto>()
                .ForMember(
                    dest => dest.SalesRepName,
                    opt => opt.MapFrom(src =>
                        src.SalesRep != null
                            ? src.SalesRep.Name
                            : null))
                .ForMember(
                    dest => dest.CustomerName,
                    opt => opt.MapFrom(src =>
                        src.Customer != null
                            ? src.Customer.Name
                            : null))
                .ForMember(
                    dest => dest.RouteName,
                    opt => opt.MapFrom(src =>
                        src.Route != null
                            ? src.Route.Name
                            : null))
                .ForMember(
                    dest => dest.InvoiceNumber,
                    opt => opt.MapFrom(src =>
                        src.Invoice != null
                            ? src.Invoice.InvoiceNumber
                            : null))
                .ForMember(
                    dest => dest.PaymentNumber,
                    opt => opt.MapFrom(src =>
                        src.Payment != null
                            ? src.Payment.PaymentNumber
                            : null))
                .ForMember(
                    dest => dest.InvoiceReturnNumber,
                    opt => opt.MapFrom(src =>
                        src.InvoiceReturn != null
                            ? src.InvoiceReturn.ReturnNumber
                            : null));

            #endregion

            #region Treasury

            CreateMap<CashBox, CashBoxDto>();

            CreateMap<CashReceipt, CashReceiptDto>();

            CreateMap<SalesRepCashLedger, SalesRepCashLedgerDto>();

            CreateMap<TreasuryTransaction, TreasuryTransactionDto>();

            #endregion

            #region UnloadRequest 

            CreateMap<SalesRepUnloadRequest, SalesRepUnloadRequestDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<SalesRepUnloadRequestItem, SalesRepUnloadRequestItemDto>();

            #endregion
        }
    }
}
