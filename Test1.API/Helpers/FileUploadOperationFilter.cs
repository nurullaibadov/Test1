using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Test1.API.Helpers
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) ||
                           p.ParameterType == typeof(List<IFormFile>) ||
                           p.ParameterType == typeof(IEnumerable<IFormFile>) ||
                           p.ParameterType == typeof(IFormFileCollection))
                .ToList();

            if (!fileParameters.Any())
                return;

            // Remove existing parameters
            operation.Parameters?.Clear();

            var queryParameters = context.MethodInfo.GetParameters()
                .Where(p => p.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.FromQueryAttribute), false).Any())
                .ToList();

            // Add query parameters back
            foreach (var queryParam in queryParameters)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = queryParam.Name,
                    In = ParameterLocation.Query,
                    Required = !queryParam.HasDefaultValue,
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });
            }

            // Create schema properties for file upload
            var properties = new Dictionary<string, OpenApiSchema>();
            var required = new HashSet<string>();

            foreach (var fileParam in fileParameters)
            {
                if (fileParam.ParameterType == typeof(IFormFile))
                {
                    properties.Add(fileParam.Name ?? "file", new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    });
                    required.Add(fileParam.Name ?? "file");
                }
                else if (fileParam.ParameterType == typeof(List<IFormFile>) ||
                         fileParam.ParameterType == typeof(IEnumerable<IFormFile>) ||
                         fileParam.ParameterType == typeof(IFormFileCollection))
                {
                    properties.Add(fileParam.Name ?? "files", new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        }
                    });
                    required.Add(fileParam.Name ?? "files");
                }
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties,
                            Required = required
                        }
                    }
                }
            };
        }
    }
}