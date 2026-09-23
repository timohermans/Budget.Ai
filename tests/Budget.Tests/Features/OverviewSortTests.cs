using Budget.Web.Features.Budget;

namespace Budget.Tests.Features;

[TestClass]
public class OverviewSortTests
{
    [TestMethod]
    [DataRow("datum-begin-eind", OverviewSort.DateAsc)]
    [DataRow("hoogste-uitgaves", OverviewSort.AmountDescHoogsteUitgaves)]
    [DataRow("kleinste-uitgaves", OverviewSort.AmountAscKleinsteUitgaves)]
    [DataRow("winkel-a-z", OverviewSort.NameAsc)]
    [DataRow("winkel-z-a", OverviewSort.NameDesc)]
    public void Parse_WhenKnownValue_ThenReturnsSort(string value, OverviewSort expected)
    {
        Assert.AreEqual(expected, OverviewSorts.Parse(value));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("garbage")]
    [DataRow("DATUM-BEGIN-EIND")]
    [DataRow("datum-begin-eind ")]
    public void Parse_WhenUnknownValue_ThenReturnsDefault(string? value)
    {
        Assert.AreEqual(OverviewSort.DateDesc, OverviewSorts.Parse(value));
    }

    [TestMethod]
    public void ToQuery_WhenDefault_ThenReturnsNull()
    {
        Assert.IsNull(OverviewSorts.ToQuery(OverviewSort.DateDesc));
    }

    [TestMethod]
    public void ToQuery_WhenNonDefault_ThenReturnsKebabValue()
    {
        Assert.AreEqual("datum-begin-eind", OverviewSorts.ToQuery(OverviewSort.DateAsc));
        Assert.AreEqual("hoogste-uitgaves", OverviewSorts.ToQuery(OverviewSort.AmountDescHoogsteUitgaves));
        Assert.AreEqual("kleinste-uitgaves", OverviewSorts.ToQuery(OverviewSort.AmountAscKleinsteUitgaves));
        Assert.AreEqual("winkel-a-z", OverviewSorts.ToQuery(OverviewSort.NameAsc));
        Assert.AreEqual("winkel-z-a", OverviewSorts.ToQuery(OverviewSort.NameDesc));
    }

    [TestMethod]
    public void ToQuery_ThenParse_RoundTrips()
    {
        foreach (OverviewSort sort in Enum.GetValues<OverviewSort>())
            Assert.AreEqual(sort, OverviewSorts.Parse(OverviewSorts.ToQuery(sort)));
    }
}