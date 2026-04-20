using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Api.OpenApi;

    internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        In = ParameterLocation.Header,
                        BearerFormat = "Json Web Token"
                    }
                };

                foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
                {
                    operation.Value.Security ??= [];
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    });
                }
            }
        }
    }


