using FluentAssertions;
using PokManager.Domain.Enumerations;

namespace PokManager.Domain.Tests.Enumerations;

public class OperationTypeTests
{
    [Fact]
    public void OperationType_Should_Have_Correct_Values()
    {
        OperationType.Unknown.Should().Be((OperationType)0);
        OperationType.StartInstance.Should().Be((OperationType)1);
        OperationType.StopInstance.Should().Be((OperationType)2);
        OperationType.RestartInstance.Should().Be((OperationType)3);
        OperationType.CreateBackup.Should().Be((OperationType)4);
        OperationType.RestoreBackup.Should().Be((OperationType)5);
        OperationType.DeleteBackup.Should().Be((OperationType)6);
        OperationType.ApplyConfiguration.Should().Be((OperationType)7);
        OperationType.CheckUpdates.Should().Be((OperationType)8);
        OperationType.ApplyUpdates.Should().Be((OperationType)9);
        OperationType.CreateInstance.Should().Be((OperationType)10);
        OperationType.DeleteInstance.Should().Be((OperationType)11);
        OperationType.SaveWorld.Should().Be((OperationType)12);
        OperationType.SendChatMessage.Should().Be((OperationType)13);
        OperationType.ExecuteCustomCommand.Should().Be((OperationType)14);
    }

    [Fact]
    public void OperationType_Should_Have_All_Expected_Members()
    {
        var values = Enum.GetValues<OperationType>();
        values.Should().Contain(OperationType.Unknown);
        values.Should().Contain(OperationType.StartInstance);
        values.Should().Contain(OperationType.StopInstance);
        values.Should().Contain(OperationType.RestartInstance);
        values.Should().Contain(OperationType.CreateBackup);
        values.Should().Contain(OperationType.RestoreBackup);
        values.Should().Contain(OperationType.DeleteBackup);
        values.Should().Contain(OperationType.ApplyConfiguration);
        values.Should().Contain(OperationType.CheckUpdates);
        values.Should().Contain(OperationType.ApplyUpdates);
        values.Should().Contain(OperationType.CreateInstance);
        values.Should().Contain(OperationType.DeleteInstance);
        values.Should().Contain(OperationType.SaveWorld);
        values.Should().Contain(OperationType.SendChatMessage);
        values.Should().Contain(OperationType.ExecuteCustomCommand);
    }
}
