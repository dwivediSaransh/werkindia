using IndiaWorks.Domain.E03;
using Xunit;

namespace IndiaWorks.Domain.Tests.E03;

public sealed class GeographyTests
{
    [Fact]
    public void Create_ValidCountry_SetsDefaults()
    {
        var geography = Geography.Create(GeographyType.Country, "India", "IN");

        Assert.NotEqual(Guid.Empty, geography.Id);
        Assert.Equal(GeographyType.Country, geography.Type);
        Assert.Equal("India", geography.Name);
        Assert.Equal("IN", geography.Code);
        Assert.Null(geography.ParentId);
        Assert.Null(geography.Parent);
        Assert.Empty(geography.Children);
        Assert.Null(geography.Latitude);
        Assert.Null(geography.Longitude);
        Assert.NotEqual(default, geography.CreatedAt);
    }

    [Fact]
    public void Create_WhitespacedNameAndCode_TrimsThem()
    {
        var geography = Geography.Create(GeographyType.State, "  Karnataka  ", "  KA  ");

        Assert.Equal("Karnataka", geography.Name);
        Assert.Equal("KA", geography.Code);
    }

    [Fact]
    public void Create_NullableCode_KeepsItNull()
    {
        var geography = Geography.Create(GeographyType.District, "Ranga Reddy", null);

        Assert.Null(geography.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_EmptyOrNullName_ThrowsArgumentException(string? name)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            Geography.Create(GeographyType.City, name!, "CITY"));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void CreateWithCoordinates_SetsBothValues()
    {
        var geography = Geography.CreateWithCoordinates(
            GeographyType.Pin,
            "560001",
            "560001",
            Guid.Empty,
            12.9716m,
            77.5946m);

        Assert.Equal(12.9716m, geography.Latitude);
        Assert.Equal(77.5946m, geography.Longitude);
    }

    [Fact]
    public void ReparentTo_ChangesParentId()
    {
        var parent = Geography.Create(GeographyType.Country, "India", "IN");
        var child = Geography.Create(GeographyType.State, "Karnataka", "KA", parent.Id);

        child.ReparentTo(Guid.NewGuid());

        Assert.NotEqual(parent.Id, child.ParentId);
    }

    [Fact]
    public void ReparentTo_NullParentId_ClearsParent()
    {
        var parent = Geography.Create(GeographyType.Country, "India", "IN");
        var child = Geography.Create(GeographyType.State, "Karnataka", "KA", parent.Id);

        child.ReparentTo(null);

        Assert.Null(child.ParentId);
    }

    [Fact]
    public void ReparentTo_SelfReference_ThrowsArgumentException()
    {
        var geography = Geography.Create(GeographyType.City, "Bengaluru", "BLR");

        var exception = Assert.Throws<ArgumentException>(() =>
            geography.ReparentTo(geography.Id));

        Assert.Equal("newParentId", exception.ParamName);
    }
}
