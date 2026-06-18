using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.SalesRep.Entities
{
    public class SalesRep : BaseEntity
    {
        public required string SalesRepCode{ get; set; }       
        public required string Name { get; set; }              
        public required string Phone { get; set; }             
        public int BranchId { get; set; }                      
        public string? Mobile { get; set; }                    
        public string? Email { get; set; }                     
        public string? Address { get; set; }                   
        public string? CategoryCode { get; set; }              
        public SalesRepType? SalesRepType { get; set; }        
        public string? UserId { get; set; }                    
        public decimal? CreditLimit { get; set; }              
        public int? OutOfRouteLimit { get; set; }              
        public int? OutOfOrderLimit { get; set; }              
        public bool AllowCreditOverride { get; set; }          
        public bool ProofOfVisit { get; set; }                 
        public int? MaxVisitsWithoutProof { get; set; }        
        public int? SupervisorId { get; set; }                 
        public int? BusinessUnitId { get; set; }     
        

        // Navigation Properties
        public Branch Branch { get; set; } = null!;            
        public SalesRep? Supervisor { get; set; }              
        public ICollection<SalesRep> TeamMembers { get; set; } = new List<SalesRep>();
        public ICollection<SalesRepRoute> SalesRepRoutes { get; set; } = new List<SalesRepRoute>();

    }
}
