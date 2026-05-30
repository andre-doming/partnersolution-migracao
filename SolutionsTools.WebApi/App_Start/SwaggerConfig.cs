using System.Web.Http;
using WebActivatorEx;
using Swashbuckle.Application;
using Swashbuckle.Swagger;
using System;
using System.Linq;
using System.Web.Http.Description;
using System.Collections.Generic;
using SolutionsTools.WebApi;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace SolutionsTools.WebApi
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "Solutions Tools Api");
                    c.IncludeXmlComments($@"{AppDomain.CurrentDomain.BaseDirectory}\bin\SwaggerSolution.Services.xml");
                    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                    c.OperationFilter<SwaggerFilter>();
                    c.PrettyPrint();
                    c.IgnoreObsoleteActions();
                    c.IgnoreObsoleteProperties();
                })
                .EnableSwaggerUi(c =>
                {
                    c.DisableValidator();
                    c.DocExpansion(DocExpansion.List);
                    c.InjectStylesheet(thisAssembly, "swagger-custom.css");
                });
        }

        public class SwaggerFilter : IOperationFilter
        {
            public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
            {
                var toBeAuthorize = apiDescription.GetControllerAndActionAttributes<AuthorizeAttribute>().Any();

                if (toBeAuthorize)
                {
                    if (operation.parameters == null)
                        operation.parameters = new List<Parameter>();

                    operation.parameters.Add(new Parameter()
                    {
                        name = "Authorization",
                        @in = "header",
                        description = "bearer token",
                        required = true,
                        type = "string"
                    });
                }
            }
        }

        public class ExamplesOperationFilter : IOperationFilter
        {
            public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
            {
                var toBeAuthorize = apiDescription.GetControllerAndActionAttributes<AuthorizeAttribute>().Any();

                if (toBeAuthorize)
                {
                    if (operation.parameters == null)
                        operation.parameters = new List<Parameter>();

                    operation.parameters.Add(new Parameter()
                    {
                        name = "teste name",
                        @in = "teste in",
                        description = "teste descr",
                        required = false
                    });
                }
            }
        }


    }
}
