namespace Budget.E2e.Tests;

public static class TestConstants
{
    public const int Year = 2099;
    public const int Month = 1;
    public static DateOnly FirstDay => new(Year, Month, 1);
    public static DateOnly LastMonth => FirstDay.AddMonths(-1);
    public const string TestIban = "NL12RABO0123456789";
    public const string SavingsIban = "NL12RABO0987654321";
}
