using HotChocolate;
using HotChocolate.Types;

namespace IndiaWorks.Api.GraphQL.E00;

public sealed class Query
{
    public string Version() => "v0.0.1";

    public string Health() => "OK";
}