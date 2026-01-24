using FluentAssertions;
using PokManager.Domain.Common;
using PokManager.Infrastructure.PokManager.PokManager.Parsers;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Parsers;

/// <summary>
/// Tests for DetailsOutputParser using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class DetailsOutputParserTests
{
    private readonly DetailsOutputParser _parser = new();
    private Result<Dictionary<string, string>> _result = null!;

    [Fact]
    public void Given_BasicDetailsOutput_When_Parse_Then_ReturnsAllSettings()
    {
        // Given
        var output = @"SessionName: MyServer
ServerPassword: secret123
MaxPlayers: 20
ServerMap: TheIsland";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().ContainKey("SessionName");
        _result.Value["SessionName"].Should().Be("MyServer");
        _result.Value.Should().ContainKey("ServerPassword");
        _result.Value["ServerPassword"].Should().Be("secret123");
        _result.Value.Should().ContainKey("MaxPlayers");
        _result.Value["MaxPlayers"].Should().Be("20");
        _result.Value.Should().ContainKey("ServerMap");
        _result.Value["ServerMap"].Should().Be("TheIsland");
    }

    [Fact]
    public void Given_OutputWithMods_When_Parse_Then_ParsesModsList()
    {
        // Given
        var output = @"SessionName: MyServer
Mods: 12345,67890,11111";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().ContainKey("Mods");
        _result.Value["Mods"].Should().Be("12345,67890,11111");
    }

    [Fact]
    public void Given_OutputWithCustomSettings_When_Parse_Then_ParsesCustomSettings()
    {
        // Given
        var output = @"SessionName: MyServer
MaxPlayers: 32
CustomSetting1: Value1
CustomSetting2: Value2
AnotherCustom: test";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().ContainKey("CustomSetting1");
        _result.Value["CustomSetting1"].Should().Be("Value1");
        _result.Value.Should().ContainKey("CustomSetting2");
        _result.Value["CustomSetting2"].Should().Be("Value2");
        _result.Value.Should().ContainKey("AnotherCustom");
        _result.Value["AnotherCustom"].Should().Be("test");
    }

    [Fact]
    public void Given_OutputWithEmptyValues_When_Parse_Then_PreservesEmptyValues()
    {
        // Given
        var output = @"SessionName: MyServer
ServerPassword:
MaxPlayers: 20";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().ContainKey("ServerPassword");
        _result.Value["ServerPassword"].Should().Be("");
    }

    [Fact]
    public void Given_OutputWithExtraWhitespace_When_Parse_Then_TrimsWhitespace()
    {
        // Given
        var output = @"SessionName:    MyServer
ServerPassword:   secret123
MaxPlayers:   20   ";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value["SessionName"].Should().Be("MyServer");
        _result.Value["ServerPassword"].Should().Be("secret123");
        _result.Value["MaxPlayers"].Should().Be("20");
    }

    [Fact]
    public void Given_OutputWithColonInValue_When_Parse_Then_PreservesValueWithColon()
    {
        // Given
        var output = @"SessionName: MyServer
ConnectionString: server:localhost:8211
Path: C:\Program Files\Game";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value["ConnectionString"].Should().Be("server:localhost:8211");
        _result.Value["Path"].Should().Be(@"C:\Program Files\Game");
    }

    [Fact]
    public void Given_OutputWithBlankLines_When_Parse_Then_SkipsBlankLines()
    {
        // Given
        var output = @"SessionName: MyServer

ServerPassword: secret123

MaxPlayers: 20";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value.Should().ContainKey("SessionName");
        _result.Value.Should().ContainKey("ServerPassword");
        _result.Value.Should().ContainKey("MaxPlayers");
    }

    [Fact]
    public void Given_OutputWithComments_When_Parse_Then_SkipsCommentLines()
    {
        // Given
        var output = @"# Configuration file
SessionName: MyServer
# Password settings
ServerPassword: secret123
MaxPlayers: 20";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value.Should().NotContainKey("# Configuration file");
    }

    [Fact]
    public void Given_EmptyString_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("empty");
    }

    [Fact]
    public void Given_NullString_When_Parse_Then_ReturnsFailure()
    {
        // Given
        string output = null!;

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("null or empty");
    }

    [Fact]
    public void Given_OnlyBlankLines_When_Parse_Then_ReturnsEmptyDictionary()
    {
        // Given
        var output = @"


";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_LineWithoutColon_When_Parse_Then_SkipsInvalidLine()
    {
        // Given
        var output = @"SessionName: MyServer
This line has no colon
MaxPlayers: 20";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(2);
        _result.Value.Should().ContainKey("SessionName");
        _result.Value.Should().ContainKey("MaxPlayers");
    }

    [Fact]
    public void Given_DuplicateKeys_When_Parse_Then_UsesLastValue()
    {
        // Given
        var output = @"SessionName: FirstValue
SessionName: SecondValue
MaxPlayers: 20";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value["SessionName"].Should().Be("SecondValue");
    }

    [Fact]
    public void Given_NumericValues_When_Parse_Then_PreservesAsStrings()
    {
        // Given
        var output = @"MaxPlayers: 32
Port: 8211
Timeout: 300";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value["MaxPlayers"].Should().Be("32");
        _result.Value["Port"].Should().Be("8211");
        _result.Value["Timeout"].Should().Be("300");
    }

    [Fact]
    public void Given_BooleanValues_When_Parse_Then_PreservesAsStrings()
    {
        // Given
        var output = @"EnablePVP: true
AllowFlight: false
AutoSave: True";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value["EnablePVP"].Should().Be("true");
        _result.Value["AllowFlight"].Should().Be("false");
        _result.Value["AutoSave"].Should().Be("True");
    }

    [Fact]
    public void Given_MixedCaseKeys_When_Parse_Then_PreservesCasing()
    {
        // Given
        var output = @"SessionName: MyServer
sessionname: myserver
SESSIONNAME: MYSERVER";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        // Last one wins due to duplicate handling
        _result.Value.Should().ContainKey("SESSIONNAME");
    }

    [Fact]
    public void Given_ComplexRealWorldOutput_When_Parse_Then_ParsesAllFields()
    {
        // Given
        var output = @"SessionName: PalWorld_Production
ServerPassword: MySecurePassword123
AdminPassword: AdminPass456
MaxPlayers: 32
Port: 8211
ServerMap: Palworld/Maps/Palworld_Main
Difficulty: Normal
DayTimeSpeedRate: 1.000000
NightTimeSpeedRate: 1.000000
ExpRate: 1.000000
PalCaptureRate: 1.000000
PalSpawnNumRate: 1.000000
DeathPenalty: All
bEnablePlayerToPlayerDamage: False
bEnableFriendlyFire: False
bEnableInvaderEnemy: True
CustomSetting_XP: 2.5
CustomSetting_Gather: 3.0";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(18);
        _result.Value["SessionName"].Should().Be("PalWorld_Production");
        _result.Value["MaxPlayers"].Should().Be("32");
        _result.Value["Port"].Should().Be("8211");
        _result.Value["CustomSetting_XP"].Should().Be("2.5");
        _result.Value["CustomSetting_Gather"].Should().Be("3.0");
    }

    [Fact]
    public void Given_ErrorMessage_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "Error: Instance not found";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("Instance not found");
    }

    [Fact]
    public void Given_OutputWithTabSeparators_When_Parse_Then_ParsesCorrectly()
    {
        // Given
        var output = "SessionName:\tMyServer\nMaxPlayers:\t20";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value["SessionName"].Should().Be("MyServer");
        _result.Value["MaxPlayers"].Should().Be("20");
    }
}
