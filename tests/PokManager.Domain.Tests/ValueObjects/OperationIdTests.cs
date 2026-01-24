using FluentAssertions;
using PokManager.Domain.ValueObjects;

namespace PokManager.Domain.Tests.ValueObjects;

public class OperationIdTests
{
    [Fact]
    public void New_OperationId_Should_Generate_Unique_Id()
    {
        var id1 = OperationId.New();
        var id2 = OperationId.New();
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Create_From_Valid_Guid_Should_Succeed()
    {
        var guid = Guid.NewGuid();
        var result = OperationId.Create(guid);
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_From_Empty_Guid_Should_Fail()
    {
        var result = OperationId.Create(Guid.Empty);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Parse_Valid_String_Should_Succeed()
    {
        var guid = Guid.NewGuid();
        var result = OperationId.Parse(guid.ToString());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Parse_Invalid_String_Should_Fail()
    {
        var result = OperationId.Parse("not-a-guid");
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Parse_Empty_String_Should_Fail()
    {
        var result = OperationId.Parse(string.Empty);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void OperationIds_With_Same_Guid_Should_Be_Equal()
    {
        var guid = Guid.NewGuid();
        var id1 = OperationId.Create(guid).Value;
        var id2 = OperationId.Create(guid).Value;
        id1.Should().Be(id2);
    }

    [Fact]
    public void ToString_Should_Return_Guid_String()
    {
        var guid = Guid.NewGuid();
        var id = OperationId.Create(guid).Value;
        id.ToString().Should().Be(guid.ToString());
    }
}
