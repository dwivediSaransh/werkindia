using HotChocolate.Types;

namespace IndiaWorks.Api.GraphQL.E02.Types;

/// <summary>
/// GraphQL enum type for EmployerSectorInput.
/// Values are exposed in PascalCase to match the API contract: Private, Government, Psu, Ngo, Household.
/// </summary>
public sealed class EmployerSectorInputType : EnumType<EmployerSectorInput>
{
    protected override void Configure(IEnumTypeDescriptor<EmployerSectorInput> descriptor)
    {
        descriptor.Name("EmployerSectorInput");
        descriptor.Value(EmployerSectorInput.Private).Name("Private");
        descriptor.Value(EmployerSectorInput.Government).Name("Government");
        descriptor.Value(EmployerSectorInput.Psu).Name("Psu");
        descriptor.Value(EmployerSectorInput.Ngo).Name("Ngo");
        descriptor.Value(EmployerSectorInput.Household).Name("Household");
    }
}
