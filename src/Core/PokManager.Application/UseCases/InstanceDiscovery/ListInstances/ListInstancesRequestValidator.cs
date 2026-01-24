using FluentValidation;

namespace PokManager.Application.UseCases.InstanceDiscovery.ListInstances;

public class ListInstancesRequestValidator : AbstractValidator<ListInstancesRequest>
{
    public ListInstancesRequestValidator()
    {
        // No validation rules needed for empty request
    }
}
