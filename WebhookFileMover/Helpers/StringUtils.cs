using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;
using WebhookFileMover.Models;

namespace WebhookFileMover.Helpers
{
    public static class StringUtils
    {
        private static readonly Regex InvalidFileNameChars = new("[\\\\/:\"*?<>|'`]+");

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

                var subst = strings.FirstOrDefault(x => x.Item1.Equals(c)).Item2;
                stringBuilder.Append(subst);
            }

            return stringBuilder.ToString();
        }

        private static string FolderNameTransformationFunc(ZoomWebhook webhookEvent)
        {
            DateTimeZone usingTimeZone;
            try
            {
                usingTimeZone =
                    DateTimeZoneProviders.Tzdb[webhookEvent.Payload?.Object?.Timezone ?? "America/Los_Angeles"];
            }
            catch (DateTimeZoneNotFoundException)
            {
                usingTimeZone = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];
            }

            var offset =
                usingTimeZone.GetUtcOffset(webhookEvent.Payload?.Object?.StartTime.ToInstant() ?? new Instant());
            var offsetSpan = offset.ToTimeSpan();
            string st =
                $"{webhookEvent?.Payload?.Object?.StartTime.UtcDateTime.Add(offsetSpan).ToString("yy_MM_dd-HHmm-", CultureInfo.InvariantCulture)}{webhookEvent?.Payload?.Object?.Topic ?? "Recording"}-{webhookEvent?.Payload?.Object?.HostEmail ?? webhookEvent?.Payload?.AccountId ?? string.Empty}";
            return InvalidFileNameChars.Replace(st, string.Empty).Replace(" ", "_");
        }
    }
}