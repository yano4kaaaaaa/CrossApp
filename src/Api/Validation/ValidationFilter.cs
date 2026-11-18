using FluentValidation;

namespace Api.Validation;

// Endpoint filter: перед викликом обробника валідує аргумент типу T.
// Помилки -> 400 ValidationProblem зі словником { поле: [повідомлення] }.
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        T? argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Тіло запиту відсутнє.");

        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(context);

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(context);
    }
}