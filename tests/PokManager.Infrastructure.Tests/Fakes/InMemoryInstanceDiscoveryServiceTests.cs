using FluentAssertions;
using Xunit;

namespace PokManager.Infrastructure.Tests.Fakes;

public class InMemoryInstanceDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverInstances_Returns_Empty_List_When_No_Instances()
    {
        var service = new InMemoryInstanceDiscoveryService();

        var result = await service.DiscoverInstancesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverInstances_Returns_Added_Instances()
    {
        var service = new InMemoryInstanceDiscoveryService();
        service.AddInstance("island_main");
        service.AddInstance("scorched_pvp");

        var result = await service.DiscoverInstancesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("island_main");
        result.Value.Should().Contain("scorched_pvp");
    }

    [Fact]
    public async Task ExistsAsync_Returns_True_For_Added_Instance()
    {
        var service = new InMemoryInstanceDiscoveryService();
        service.AddInstance("island_main");

        var exists = await service.ExistsAsync("island_main");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_Returns_False_For_Missing_Instance()
    {
        var service = new InMemoryInstanceDiscoveryService();

        var exists = await service.ExistsAsync("nonexistent");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateCacheAsync_Invalidates_Cache()
    {
        var service = new InMemoryInstanceDiscoveryService();

        await service.InvalidateCacheAsync();

        service.IsCacheValid().Should().BeFalse();
    }

    [Fact]
    public async Task DiscoverInstancesAsync_Revalidates_Cache()
    {
        var service = new InMemoryInstanceDiscoveryService();
        await service.InvalidateCacheAsync();

        await service.DiscoverInstancesAsync();

        service.IsCacheValid().Should().BeTrue();
    }

    [Fact]
    public async Task AddInstance_Does_Not_Add_Duplicates()
    {
        var service = new InMemoryInstanceDiscoveryService();
        service.AddInstance("island_main");
        service.AddInstance("island_main");

        var result = await service.DiscoverInstancesAsync();

        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveInstance_Removes_Instance()
    {
        var service = new InMemoryInstanceDiscoveryService();
        service.AddInstance("island_main");

        service.RemoveInstance("island_main");

        var result = await service.DiscoverInstancesAsync();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Reset_Clears_All_Instances()
    {
        var service = new InMemoryInstanceDiscoveryService();
        service.AddInstance("island_main");
        service.AddInstance("scorched_pvp");

        service.Reset();

        var result = await service.DiscoverInstancesAsync();
        result.Value.Should().BeEmpty();
    }
}
