# Backend implementation patterns

## Feature slice

Use:
`IndiaWorks.Application/Features/<Feature>/<UseCase>/`

A use case normally contains:

- `<UseCase>Command.cs` or request record
- `<UseCase>Result.cs`
- `<UseCase>Handler.cs`
- `<UseCase>Validator.cs` when validation is non-trivial

Do not introduce MediatR merely to dispatch one method. Handlers are DI services.

## Result pattern

```csharp
public sealed record AppError(string Code, string Message, string? Field = null);

public sealed record AppResult<T>(T? Value, IReadOnlyList<AppError> Errors)
{
    public bool IsSuccess => Errors.Count == 0;
    public static AppResult<T> Ok(T value) => new(value, []);
    public static AppResult<T> Fail(params AppError[] errors) => new(default, errors);
}
```

## EF configuration

```csharp
internal sealed class ExampleConfiguration : IEntityTypeConfiguration<Example>
{
    public void Configure(EntityTypeBuilder<Example> b)
    {
        b.ToTable("Example");
        b.HasKey(x => x.Id);
        b.Property(x => x.CreateDate).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
```

## GraphQL feature extension

```csharp
[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class ExampleMutations
{
    public async Task<ExamplePayload> DoExample(
        ExampleInput input,
        [Service] ExampleHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(input, ct);
        return ExamplePayload.From(result);
    }
}
```

## REST feature endpoint

```csharp
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapPost("/otp/request", RequestOtp);
        return app;
    }
}
```

## Database rule

Every schema change requires:

1. domain entity/configuration change,
2. `dotnet ef migrations add <Name>`,
3. review migration,
4. integration test applying migration to empty SQL Server,
5. no hand-maintained parallel SQL schema.
