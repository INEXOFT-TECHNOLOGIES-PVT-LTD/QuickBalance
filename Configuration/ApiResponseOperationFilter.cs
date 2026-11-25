
using Microsoft.OpenApi;
using QuickBalance.API.CLS;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QuickBalance.API.Configuration
{
    public sealed class ApiResponseOperationFilter : IOperationFilter
    {
        public void Apply(
            OpenApiOperation operation,
            OperationFilterContext context)
        {
            var supportedResponses =
                context.ApiDescription.SupportedResponseTypes;

            foreach (var response in supportedResponses)
            {
                var responseType = response.Type;

                if (responseType == null)
                    continue;

                // Only process error responses.
                if (response.StatusCode == StatusCodes.Status200OK)
                    continue;

                bool isErrorResponse =
                    responseType == typeof(ApiResponseWithError);

                bool isGenericResponse =
                    responseType.IsGenericType &&
                    (
                        responseType.GetGenericTypeDefinition() ==
                            typeof(ApiResponse<>) ||
                        responseType.GetGenericTypeDefinition() ==
                            typeof(ApiResponseWithoutPagination<>)
                    );

                if (!isErrorResponse && !isGenericResponse)
                    continue;

                if (!operation.Responses.TryGetValue(
                    response.StatusCode.ToString(),
                    out var apiResponse))
                {
                    continue;
                }

                foreach (var content in apiResponse.Content.Values)
                {
                    content.Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,

                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["status"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Boolean,
                                Default = false
                            },

                            ["message"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String
                            }
                        },

                        Required = new HashSet<string>
                        {
                            "status",
                            "message"
                        }
                    };
                }
            }
        }
    }
}
