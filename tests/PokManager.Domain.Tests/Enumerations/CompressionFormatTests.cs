using FluentAssertions;
using PokManager.Domain.Enumerations;

namespace PokManager.Domain.Tests.Enumerations;

public class CompressionFormatTests
{
    [Fact]
    public void CompressionFormat_Should_Have_Correct_Values()
    {
        CompressionFormat.Unknown.Should().Be((CompressionFormat)0);
        CompressionFormat.Gzip.Should().Be((CompressionFormat)1);
        CompressionFormat.Zstd.Should().Be((CompressionFormat)2);
    }

    [Fact]
    public void CompressionFormat_Should_Have_All_Expected_Members()
    {
        var values = Enum.GetValues<CompressionFormat>();
        values.Should().Contain(CompressionFormat.Unknown);
        values.Should().Contain(CompressionFormat.Gzip);
        values.Should().Contain(CompressionFormat.Zstd);
    }
}
