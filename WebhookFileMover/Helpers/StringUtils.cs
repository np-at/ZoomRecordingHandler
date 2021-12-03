using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Json.Path;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebhookFileMover.Helpers
{
    public class TemplateResolverService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TemplateResolverService> _logger;

        public TemplateResolverService(IServiceProvider serviceProvider, ILogger<TemplateResolverService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private async Task<string> ResolveDynamicTemplateParam(string funcInput, Type serviceType,
            string funcName)
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService(serviceType);
            try
            {
                var output = service.GetType().GetMethod(funcName);


                _logger.LogCritical("output: {@Output}", output);


                var s = output?.Invoke(service, new object?[] { funcInput }) as Task<string?> ??
                        throw new NullReferenceException($"unable to invoke dynamic template function: {funcName}");

                string? f = await s.ConfigureAwait(false);
                return f ?? string.Empty;

                
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error during dynamic invocation");
                throw;
            }
        }

        public async ValueTask<IEnumerable<(char, string)>> ResolveTemplateParams(
            Dictionary<string, dynamic>? parameterBag,
            string? jsonString, params (char, string)[] extraParams)
        {
            List<(char, string)> col = new();
            using var jsonDocument =
                JsonDocument.Parse(jsonString ?? throw new InvalidOperationException());
            foreach (string paramBagKey in parameterBag?.Keys.ToArray() ?? Array.Empty<string>())
            {
                dynamic value = parameterBag?[paramBagKey] ?? string.Empty;

                switch (value)
                {
                    case string svalue:
                        // If it starts with a '$', treat it as a JSONPath
                        if (svalue.StartsWith('$'))
                        {
                            var jsonPath = JsonPath.Parse(svalue);

                            var results = jsonPath.Evaluate(jsonDocument.RootElement);
                            string? val = results.Matches?.SingleOrDefault()?.Value.GetString();
                            if (val != null)
                                col.Add((paramBagKey[1], val));
                        }
                        // Treat values that start with ! as function lookup keys
                        else if (svalue.StartsWith('!'))
                        {
                            if (!StringUtils._dictionary.Any())
                                throw new InvalidOperationException(
                                    "Template function is attempting to be called but no stored functions are registered for use");
                            // the value is split into two parts seperated by the | character.
                            // The first is the name of the function to lookup
                            // The second is a JSONPath expression telling us what use as the function parameter
                            string[] parts = svalue.TrimStart('!').Split('|', 2);
                            string funcName = parts[0];
                            string funcParameterLookupKey = parts[1];
                            if (!StringUtils._dictionary.TryGetValue(funcName, out var func))
                                throw new KeyNotFoundException(
                                    $"Unable to find registered template function with name {funcName}");
                            var pathResult = JsonPath.Parse(funcParameterLookupKey).Evaluate(jsonDocument.RootElement);
                            string resolvedPathResult =
                                pathResult.Matches?.FirstOrDefault()?.Value.GetString() ?? string.Empty;
                            var val = await ResolveDynamicTemplateParam(resolvedPathResult, func.Item1, func.Item2);

                            col.Add((paramBagKey[1], val));
                        }
                        else
                        {
                            // if not a JSONPath expression, take it to be a literal replacement
                            col.Add((paramBagKey[1], svalue));
                        }

                        break;
                    case int number:
                        col.Add((paramBagKey[1], number.ToString()));
                        break;
                    default:
                        throw new NotImplementedException("support for non-string types is not yet implemented");
                }
            }

            col.AddRange(extraParams);
            return col;
        }
    }

    public class TypedKeyValuePair<T>
    {
        public KeyValuePair<T, Func<T, Task<string>>> CreateInstance(T item, Func<T, Task<string>> func)
        {
            return KeyValuePair.Create(item, func);
        }
    }

    public static class StringUtils
    {
        private static readonly Regex InvalidFileNameChars = new("[\\\\/:\"*?<>|'`]+");
        internal static ConcurrentDictionary<string, (Type, string)> _dictionary = new();

        public static bool AddLookupFuncToDict<T>(string func)
        {
            return _dictionary.TryAdd(func, (typeof(T), func));
        }

        public static readonly (char, string)[] DefaultTemplateVals =
        {
            ('D', DateTime.Now.ToString("yyyy_MM_dd-HHmm"))
        };


        public static string ApplyTemplatedFormattingString(string templateString, IEnumerable<(char, string)> strings)
        {
            StringBuilder stringBuilder = new();
            bool lastCharWasFn = false;
            foreach (char c in templateString.AsSpan())
            {
                if (c.Equals('%'))
                {
                    if (lastCharWasFn)
                    {
                        stringBuilder.Append('%');
                        lastCharWasFn = false;
                        continue;
                    }

                    lastCharWasFn = true;
                    continue;
                }

                if (!lastCharWasFn)
                {
                    stringBuilder.Append(c);
                    continue;
                }

                lastCharWasFn = false;

                var valueTuples = strings.ToArray(); //as (char, string)[] ?? strings.ToArray();
                string subst = valueTuples.FirstOrDefault(x => x.Item1.Equals(c)).Item2;
                stringBuilder.Append(subst);
            }

            return stringBuilder.ToString();
        }

        // private static string FolderNameTransformationFunc(ZoomWebhook webhookEvent)
        // {
        //     DateTimeZone usingTimeZone;
        //     try
        //     {
        //         usingTimeZone =
        //             DateTimeZoneProviders.Tzdb[webhookEvent.Payload?.Object?.Timezone ?? "America/Los_Angeles"];
        //     }
        //     catch (DateTimeZoneNotFoundException)
        //     {
        //         usingTimeZone = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];
        //     }
        //
        //     var offset =
        //         usingTimeZone.GetUtcOffset(webhookEvent.Payload?.Object?.StartTime.ToInstant() ?? new Instant());
        //     var offsetSpan = offset.ToTimeSpan();
        //     string st =
        //         $"{webhookEvent.Payload?.Object?.StartTime.UtcDateTime.Add(offsetSpan).ToString("yy_MM_dd-HHmm-", CultureInfo.InvariantCulture)}{webhookEvent.Payload?.Object?.Topic ?? "Recording"}-{webhookEvent?.Payload?.Object?.HostEmail ?? webhookEvent?.Payload?.AccountId ?? string.Empty}";
        //     return InvalidFileNameChars.Replace(st, string.Empty).Replace(" ", "_");
        // }
    }
}