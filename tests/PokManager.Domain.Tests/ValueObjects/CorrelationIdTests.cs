using FluentAssertions;
using PokManager.Domain.ValueObjects;

namespace PokManager.Domain.Tests.ValueObjects;

public class CorrelationIdTests
{
    [Fact]
    public void New_CorrelationId_Should_Generate_Unique_Id()
    {
        var id1 = CorrelationId.New();
        var id2 = CorrelationId.New();
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Create_From_Valid_Guid_Should_Succeed()
    {
        var guid = Guid.NewGuid();
        var result = CorrelationId.Create(guid);
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_From_Empty_Guid_Should_Fail()
    {
        var result = CorrelationId.Create(Guid.Empty);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Parse_Valid_String_Should_Succeed()
    {
        var guid = Guid.NewGuid();
        var result = CorrelationId.Parse(guid.ToString());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Parse_Invalid_String_Should_Fail()
    {
        var result = CorrelationId.Parse("not-a-guid");
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Parse_Empty_String_Should_Fail()
    {
        var result = CorrelationId.Parse(string.Empty);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CorrelationIds_With_Same_Guid_Should_Be_Equal()
    {
        var guid = Guid.NewGuid();
        var id1 = CorrelationId.Create(guid).Value;
        var id2 = CorrelationId.Create(guid).Value;
        id1.Should().Be(id2);
    }

    [Fact]
    public void ToString_Should_Return_Guid_String()
    {
        var guid = Guid.NewGuid();
        var id = CorrelationId.Create(guid).Value;
        id.ToString().Should().Be(guid.ToString());
    }
}
