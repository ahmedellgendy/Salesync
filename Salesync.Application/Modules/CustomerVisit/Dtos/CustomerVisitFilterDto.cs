using Salesync.Domain.Common.Enums.CustomerVisit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Salesync.Application.Modules.CustomerVisit.Dtos
{
    public class CustomerVisitFilterDto
    {
        public int? SalesRepId { get; set; }
        public int? CustomerId { get; set; }
        public int? RouteId { get; set; }
        public int? SalesRepSessionId { get; set; }
        public VisitType? VisitType { get; set; }
        public VisitStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
