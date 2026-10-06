using HotChocolate;
using HotChocolate.Types;

namespace IndiaWorks.Api.GraphQL.E00;

public sealed class Mutation
{
    public string Echo(string input) => input;
}