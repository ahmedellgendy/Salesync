namespace Salesync.Domain.Common.Enums.UnloadRequest
{
    public enum UnloadRequestStatus
    {
        Draft = 1,              // المندوب حضّر الطلب ولسه مبعتوش
        PendingWarehouse = 2,   // اتبعت للمخزن ومستني تأكيد
        PartiallyConfirmed = 3, // المخزن استلم جزء أو فيه فروقات
        Confirmed = 4,          // المخزن أكد الكميات كاملة
        Cancelled = 5           // ملغي
    }
}