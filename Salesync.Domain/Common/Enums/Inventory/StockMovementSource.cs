namespace Salesync.Domain.Common.Enums.Inventory
{
    public enum StockMovementSource
    {
        OpeningBalance = 1,    // رصيد افتتاحي
        Invoice = 2,           // فاتورة بيع
        InvoiceReturn = 3,     // مرتجع
        LoadRequest = 4,       // طلب حمولة
        StockTransfer = 5,     // تحويل مخازن
        StockAdjustment = 6,   // تسوية / تعديل يدوي
        Purchase = 7,           // شراء / إضافة من مورد
        UnloadRequest = 8
    }
}
