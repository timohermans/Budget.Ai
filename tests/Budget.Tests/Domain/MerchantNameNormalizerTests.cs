using Budget.Web.Domain.Merchants;

namespace Budget.Tests.Domain;

[TestClass]
public class MerchantNameNormalizerTests
{
    [TestMethod]
    public void Normalize_WhenCaseDiffers_ThenSameKey()
    {
        Assert.AreEqual(
            MerchantNameNormalizer.Normalize("Albert Heijn"),
            MerchantNameNormalizer.Normalize("ALBERT HEIJN"));
    }

    [TestMethod]
    public void Normalize_WhenWhitespaceDiffers_ThenSameKey()
    {
        Assert.AreEqual(
            MerchantNameNormalizer.Normalize("Albert  Heijn"),
            MerchantNameNormalizer.Normalize("  Albert Heijn "));
    }

    [TestMethod]
    public void Normalize_WhenHyphenSpacingDiffers_ThenSameKey()
    {
        Assert.AreEqual(
            MerchantNameNormalizer.Normalize("AH- Jan Linders 4181"),
            MerchantNameNormalizer.Normalize("AH - Jan Linders 4181"));
    }

    [TestMethod]
    public void Normalize_WhenHyphenAdjacent_ThenKeyHasSpacedHyphen()
    {
        Assert.AreEqual("ah - jan linders 4181", MerchantNameNormalizer.Normalize("AH - Jan Linders 4181"));
    }

    [TestMethod]
    public void Normalize_WhenBlank_ThenEmpty()
    {
        Assert.AreEqual(string.Empty, MerchantNameNormalizer.Normalize(null));
        Assert.AreEqual(string.Empty, MerchantNameNormalizer.Normalize("   "));
    }

    [TestMethod]
    public void Normalize_WhenMerchantExample_ThenStableKey()
    {
        var variants = new[]
        {
            "AH- Jan Linders 4181",
            "AH - Jan Linders 4149",
            "Albert Heijn Online",
            "AH Grevenbicht",
            "Albert Heijn 1194",
            "Albert Heijn 1517",
            "Albert Heijn",
        };

        var keys = variants.Select(MerchantNameNormalizer.Normalize).ToHashSet();

        Assert.Contains("albert heijn", keys, "Plain name should normalize to the canonical key");
        Assert.IsTrue(keys.All(k => k.Length > 0));
    }
}
