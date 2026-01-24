using FluentAssertions;
using PokManager.Application.UseCases.InstanceLifecycle.CreateInstance;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceLifecycle.CreateInstance;

public class CreateInstanceRequestTests
{
    private readonly CreateInstanceRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_With_All_Required_Fields_Should_Pass_Validation()
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Island Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Request_With_All_Fields_Should_Pass_Validation()
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Island Server",
            MapName: "TheIsland",
            MaxPlayers: 50,
            ServerAdminPassword: "admin123",
            ServerPassword: "player123",
            GamePort: 8777,
            RconPort: 28020,
            ClusterId: "my_cluster",
            EnableApi: true,
            EnableBattlEye: false,
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail_Validation()
    {
        var request = new CreateInstanceRequest(
            InstanceId: "",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Empty_SessionName_Should_Fail_Validation()
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.SessionName));
    }

    [Theory]
    [InlineData("InvalidMap")]
    [InlineData("SomeRandomMap")]
    [InlineData("")]
    public void Invalid_MapName_Should_Fail_Validation(string mapName)
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: mapName,
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.MapName));
    }

    [Theory]
    [InlineData("TheIsland")]
    [InlineData("Ragnarok")]
    [InlineData("Valguero")]
    [InlineData("CrystalIsles")]
    [InlineData("Fjordur")]
    [InlineData("ASA_TheIsland")]
    public void Valid_MapNames_Should_Pass_Validation(string mapName)
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: mapName,
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(128)]
    [InlineData(200)]
    public void Invalid_MaxPlayers_Should_Fail_Validation(int maxPlayers)
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: maxPlayers,
            ServerAdminPassword: "admin123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.MaxPlayers));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(127)]
    public void Valid_MaxPlayers_Should_Pass_Validation(int maxPlayers)
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: maxPlayers,
            ServerAdminPassword: "admin123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("a")]
    public void ServerAdminPassword_Too_Short_Should_Fail_Validation(string password)
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: password,
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ServerAdminPassword));
    }

    [Fact]
    public void ServerAdminPassword_Too_Long_Should_Fail_Validation()
    {
        var longPassword = new string('a', 65);
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: longPassword,
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ServerAdminPassword));
    }

    [Theory]
    [InlineData("pass word")]
    [InlineData("pass@word")]
    [InlineData("pass#word")]
    public void ServerAdminPassword_With_Invalid_Characters_Should_Fail(string password)
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: password,
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ServerAdminPassword));
    }

    [Theory]
    [InlineData(500)]
    [InlineData(1023)]
    [InlineData(65536)]
    [InlineData(70000)]
    public void Invalid_GamePort_Should_Fail_Validation(int port)
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            GamePort: port,
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.GamePort));
    }

    [Theory]
    [InlineData(500)]
    [InlineData(1023)]
    [InlineData(65536)]
    [InlineData(70000)]
    public void Invalid_RconPort_Should_Fail_Validation(int port)
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            RconPort: port,
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.RconPort));
    }

    [Fact]
    public void Same_GamePort_And_RconPort_Should_Fail_Validation()
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            GamePort: 7777,
            RconPort: 7777,
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Ports");
    }

    [Fact]
    public void Invalid_ClusterId_Format_Should_Fail_Validation()
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            ClusterId: "cluster@123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ClusterId));
    }

    [Theory]
    [InlineData("my-cluster")]
    [InlineData("my_cluster")]
    [InlineData("cluster123")]
    public void Valid_ClusterId_Should_Pass_Validation(string clusterId)
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            ClusterId: clusterId,
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_CorrelationId_Should_Fail_Validation()
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            CorrelationId: "");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CorrelationId));
    }

    [Fact]
    public void SessionName_At_Max_Length_Should_Pass_Validation()
    {
        var maxSessionName = new string('a', 128);
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: maxSessionName,
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SessionName_Over_Max_Length_Should_Fail_Validation()
    {
        var tooLongSessionName = new string('a', 129);
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: tooLongSessionName,
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.SessionName));
    }

    [Fact]
    public void Optional_ServerPassword_With_Valid_Value_Should_Pass()
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            ServerPassword: "player123",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Optional_ServerPassword_With_Invalid_Characters_Should_Fail()
    {
        var request = new CreateInstanceRequest(
            InstanceId: "island_main",
            SessionName: "My Server",
            MapName: "TheIsland",
            MaxPlayers: 10,
            ServerAdminPassword: "admin123",
            ServerPassword: "pass word!",
            CorrelationId: Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ServerPassword));
    }
}
