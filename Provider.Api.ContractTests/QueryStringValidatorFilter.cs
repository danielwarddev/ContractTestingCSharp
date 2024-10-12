using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Provider.Api.ContractTests;

public class QueryStringValidatorFilter : IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var parameters = new HashSet<string>(context.ActionDescriptor.Parameters.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
        var unknownParameters = new Dictionary<string, string[]>();

        foreach (var item in context.HttpContext.Request.Query)
        {
            if (!parameters.Contains(item.Key))
            {
                unknownParameters.Add(item.Key,
                    [$"Query string \"{item.Key}\" does not bind to any parameter. " +
                     $"Valid parameter names for endpoint {context.HttpContext.Request.Path} are: {string.Join(", ", parameters)}."]);
            }
        }

        if (unknownParameters.Any())
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(unknownParameters));
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}