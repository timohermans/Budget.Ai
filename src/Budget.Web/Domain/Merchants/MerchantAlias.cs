namespace Budget.Web.Domain.Merchants;

public class MerchantAlias
{
    public int Id { get; set; }
    public required string NameNormalized { get; set; }
    public int MerchantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Merchant Merchant { get; set; } = null!;
}
