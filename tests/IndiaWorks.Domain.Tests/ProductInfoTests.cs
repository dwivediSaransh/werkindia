using IndiaWorks.Domain;

namespace IndiaWorks.Domain.Tests;

public sealed class ProductInfoTests
{
    [Fact]
    public void Product_name_is_stable_for_foundation_health_checks()
    {
        Assert.Equal("IndiaWorks", ProductInfo.Name);
    }
}
