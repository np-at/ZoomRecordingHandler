using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;

namespace WebhookFileMover.Middleware
{
    public class GraphEndpointMiddleware
    {
        // inject required services using DI
        private readonly DfaGraphWriter _graphWriter;
        private readonly EndpointDataSource _endpointData;

        public GraphEndpointMiddleware(
            RequestDelegate next,
            DfaGraphWriter graphWriter,
            EndpointDataSource endpointData)
        {
            _graphWriter = graphWriter;
            _endpointData = endpointData;
        }

        public async Task Invoke(HttpContext context)
        {
            // set the response
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain";

            // Write the response into memory
            await using (var sw = new StringWriter())
            {
                // Write the graph
                _graphWriter.Write(_endpointData, sw);
                var graph = sw.ToString();

                // Write the graph to the response
                await context.Response.WriteAsync(graph);
            }
        }
    }
    public static class GraphEndpointMiddlewareExtensions
    {
        public static IEndpointConventionBuilder MapGraphVisualisation(
            this IEndpointRouteBuilder endpoints, string pattern)
        {
            var pipeline = endpoints
                .CreateApplicationBuilder()
                .UseMiddleware<GraphEndpointMiddleware>()
                .Build();

            return endpoints.Map(pattern, pipeline).WithDisplayName("Endpoint Graph");
        }
    }
}