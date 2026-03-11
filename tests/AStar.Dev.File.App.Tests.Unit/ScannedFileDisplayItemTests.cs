using AStar.Dev.File.App.ViewModels;
using Shouldly;

namespace AStar.Dev.File.App.Tests.Unit;

public class ScannedFileDisplayItemTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(500, "500 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1_048_575, "1024.0 KB")]
    [InlineData(1_048_576, "1.0 MB")]
    [InlineData(1_572_864, "1.5 MB")]
    [InlineData(1_073_741_823, "1024.0 MB")]
    [InlineData(1_073_741_824L, "1.0 GB")]
    [InlineData(1_610_612_736L, "1.5 GB")]
    public void FormatSize_ReturnsExpectedString(long bytes, string expected)
        => ScannedFileDisplayItem.FormatSize(bytes).ShouldBe(expected);
}
