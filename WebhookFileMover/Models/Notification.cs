using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Json.Path;
using WebhookFileMover.Database.Models;
using WebhookFileMover.Helpers;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;

namespace WebhookFileMover.Models
{
    public class Notification
    {
        public NotificationProviderConfig NotificationProviderConfig { get; set; }
        public string WebEventStringBody { get; set; }
        public Job Job { get; init; }

        public string GenerateSuccessMessage()
        {
            var resolvedParamBag = this.ResolveTemplateParams();
            return StringUtils.ApplyTemplatedFormattingString(
                NotificationProviderConfig.SuccessMessageTemplate ?? throw new InvalidOperationException(),
                resolvedParamBag);
        }

        // public string GenerateFailureMessage()
        // {
        //     var resolvedParamBag = this.ResolveTemplateParams();
        //     return StringUtils.ApplyTemplatedFormattingString(
        //         NotificationProviderConfig.FailureMessageTemplate ?? throw new InvalidOperationException(), resolvedParamBag);
        // }
        private IEnumerable<(char, string)> ResolveTemplateParams()
        {
            IEnumerable<(char, string)> col = Array.Empty<(char, string)>();

            foreach (string paramBagKey in NotificationProviderConfig.ParamBag?.Keys.ToArray() ?? Array.Empty<string>())
            {
                dynamic? value = NotificationProviderConfig.ParamBag?[paramBagKey];

                switch (value)
                {
                    case string svalue:

                        if (svalue.StartsWith('$'))
                        {
                            var jsonPath = JsonPath.Parse(svalue);
                            var jsonDocument = JsonDocument.Parse(WebEventStringBody);
                            var results = jsonPath.Evaluate(jsonDocument.RootElement);
                            var val = results.Matches?.SingleOrDefault()?.Value.GetString();
                            if (val != null)
                                col = col.Append((paramBagKey[0], val));
                        }
                        else
                        {
                            // if not a JSONPath expression, take it to be a literal replacement
                            col = col.Append((paramBagKey[0], svalue));
                        }

                        break;
                    default:
                        throw new NotImplementedException("support for non-string types is not yet implemented");
                }
            }

            col = col.Append(('L',
                Job.JobTaskInstances?.First(x => !string.IsNullOrWhiteSpace(x.LocationUri)).LocationUri ??
                string.Empty));

            return col;
        }
    }
}