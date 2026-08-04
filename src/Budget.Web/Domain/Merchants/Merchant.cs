namespace Budget.Web.Domain.Merchants;

public class Merchant
{
    public int Id { get; set; }
    public required string NameNormalized { get; set; }
    public required string DisplayName { get; set; }
    public string? LogoUrl { get; set; }
    public required MerchantStatus Status { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public ICollection<MerchantAlias> Aliases { get; set; } = new List<MerchantAlias>();
}
