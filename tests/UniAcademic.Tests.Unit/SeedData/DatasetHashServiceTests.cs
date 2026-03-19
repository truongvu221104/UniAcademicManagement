using UniAcademic.Infrastructure.SeedData.Services;
using Xunit;

namespace UniAcademic.Tests.Unit.SeedData;

public sealed class DatasetHashServiceTests
{
    [Fact]
    public void ComputeHash_ShouldReturnSameHash_ForSameContent()
    {
        var service = new DatasetHashService();

        var first = service.ComputeHash("[{\"code\":\"CNTT\"}]");
        var second = service.ComputeHash("[{\"code\":\"CNTT\"}]");

        Assert.Equal(first, second);
    }
}
