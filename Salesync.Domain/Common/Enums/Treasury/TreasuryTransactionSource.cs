namespace Salesync.Domain.Common.Enums.Treasury
{
    public enum TreasuryTransactionSource
    {
        OpeningBalance = 1,
        SalesRepSettlement = 2,
        Expense = 3,
        BankDeposit = 4,
        BankWithdrawal = 5,
        Adjustment = 6,
        TransferIn = 7,
        TransferOut = 8
    }
}