using HotChocolate.Execution;

namespace IndiaWorks.Api.GraphQL.E00.Types;

public sealed class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        // Only return safe, machine-readable error codes in production
        if (error.Exception is not null)
        {
            return error
                .WithCode("INTERNAL_ERROR")
                .WithMessage("An unexpected error occurred.");
        }

        return error;
    }
}