using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums;

namespace Salesync.Domain.Modules.MasterData.Entities
{
    public class Customer : BaseEntity
    {
        // Basic informations
        public required string Name { get; set; }             
        public string? Phone { get; set; }                      
        public string? Email { get; set; }

        // Address
        public string? Country { get; set; }
        public string? Address { get; set; }                   
        public string? Area { get; set; }                   
        public string? City { get; set; }                  
        public string? District { get; set; }               
        public string? Region { get; set; }                
        public string? PostalCode { get; set; }                

        // GPS Location
        public decimal? Latitude { get; set; }                 
        public decimal? Longitude { get; set; }                 

        // Classification
        public string? CategoryCode { get; set; }               
        public string? SalesSectorCode { get; set; }           
        public string? ClassId { get; set; }                   
        public CustomerType Type { get; set; }                  

        // Payment Settings
        public bool AllowCash { get; set; } = true;             
        public bool AllowCheck { get; set; }                    
        public bool AllowCreditCard { get; set; }               
        public string? PaymentTermsCode { get; set; }           

        // Credit Management
        public decimal CreditLimit { get; set; }               
        public decimal CurrentBalance { get; set; }            
        public bool ForceCreditLimit { get; set; }              
        public decimal OrderCeiling { get; set; }               

        // Business Rules
        public bool ReturnWithoutInvoice { get; set; }          
        public bool MandatoryPhoto { get; set; }               
        public bool IsHeadOffice { get; set; }                 
        public int? HeadOfficeId { get; set; }                 

        // Tracking
        public decimal TotalPurchaseAmount { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public CustomerStatus Status { get; set; } = CustomerStatus.Active;

        // External References
        public string? AccountNumber { get; set; }              
        public string? TaxId { get; set; }                     
        public string? PriceId { get; set; }                   

        // Navigation Properties
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }


    }
}
