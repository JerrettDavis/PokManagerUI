using FluentAssertions;
using PokManager.Application.UseCases.InstanceDiscovery.ListInstances;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceDiscovery.ListInstances;

public class ListInstancesRequestTests
{
    private readonly ListInstancesRequestValidator _validator = new();

    [Fact]
    public void Request_Should_Be_Constructable()
    {
        var request = new ListInstancesRequest();
        request.Should().NotBeNull();
    }

    [Fact]
    public void Request_Should_Always_Pass_Validation()
    {
        var request = new ListInstancesRequest();
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Multiple_Instances_Should_Be_Equal()
    {
        var request1 = new ListInstancesRequest();
        var request2 = new ListInstancesRequest();
        request1.Should().Be(request2);
    }
}
