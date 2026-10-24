using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.Sales.Models;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;

namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile;

public sealed record MobilePage<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages);
public sealed record MobileReviewCheck(string Code, string Title, bool Passed);
public sealed class MobileReviewDto
{
    public required SalesRepSessionDto Session { get; init; }
    public required List<MobileReviewCheck> Checks { get; init; }
    public bool CanClose { get; init; }
    public int? WarehouseId { get; init; }
    public required List<MobileWarehouseOptionDto> Warehouses { get; init; }
    public SalesRepDayClosingDto? Settlement { get; init; }
    public required SalesRepDayClosingCalculationResult FinancialSummary { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

public sealed class MobileAttentionItemDto
{
    public required string Kind { get; init; }
    public int DocumentId { get; init; }
    public required string Reference { get; init; }
    public int SalesRepId { get; init; }
    public string? SalesRepName { get; init; }
    public string? CustomerName { get; init; }
    public DateTime CreatedAt { get; init; }
    public int Status { get; init; }
    public decimal? ExpectedCashAmount { get; init; }
    public decimal? CashVariance { get; init; }
    public int? StockVariance { get; init; }
}

public sealed record MobilePaymentTotals(decimal TotalAmount, decimal CashAmount, decimal NonCashAmount);
public sealed record MobilePaymentPage(List<Salesync.Application.Modules.Sales.Dtos.Payment.PaymentDto> Items,
    int TotalCount, int PageNumber, int PageSize, int TotalPages, MobilePaymentTotals Totals);
public sealed record MobileAttentionPage(List<MobileAttentionItemDto> Items, int TotalCount,
    int PageNumber, int PageSize, int TotalPages, Dictionary<string,int> Counts, DateTime BusinessDate, DateTime CheckedAt);
public sealed record MobileOperationReceipt(string OperationKey, string Status, int? ResourceId, string Operation, DateTime CompletedAt);
