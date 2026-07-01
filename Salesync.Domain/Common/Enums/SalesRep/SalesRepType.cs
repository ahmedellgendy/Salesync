using System.ComponentModel;

namespace Salesync.Domain.Common.Enums.SalesRep
{
    public enum SalesRepType
    {
        [Description("Van Sales Representative")]
        VanSales = 1,
        [Description("Pre Sales Representative")]
        PreSeller = 2,
        [Description("Merchandiser")]
        Merchandiser = 3
    }
}
