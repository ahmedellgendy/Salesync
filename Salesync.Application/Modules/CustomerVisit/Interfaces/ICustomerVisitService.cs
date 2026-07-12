using Salesync.Application.Modules.CustomerVisit.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Salesync.Application.Modules.CustomerVisit.Interfaces
{
    public interface ICustomerVisitService
    {
        Task<IEnumerable<CustomerVisitDto>> GetAllAsync(CustomerVisitFilterDto filter);

        Task<CustomerVisitDto> GetByIdAsync(int id);

        Task<IEnumerable<CustomerVisitDto>> GetBySalesRepAsync(int salesRepId);

        Task<IEnumerable<CustomerVisitDto>> GetByCustomerAsync(int customerId);

        Task<CustomerVisitDto> StartAsync(StartCustomerVisitDto dto);

        Task<CustomerVisitDto> CompleteAsync(int id, CompleteCustomerVisitDto dto);

        Task CancelAsync(int id);
    }
}
