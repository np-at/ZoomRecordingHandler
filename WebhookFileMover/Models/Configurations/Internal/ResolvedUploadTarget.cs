using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;

namespace WebhookFileMover.Models.Configurations.Internal
{
    public record ResolvedUploadTarget
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public UploadTarget UploadTarget { get; set; }
        public UploadTargetConfig UploadTargetConfig { get; set; }

        public ResolvedUploadJob CreateUploadJob(FileInfo fileInfo)
        {
            var job = new ResolvedUploadJob(fileInfo)
            {
                Id = Id,
                UploadTarget = UploadTarget,
                UploadTargetConfig = UploadTargetConfig
            };
            return job;
        }
    }

    public record ResolvedUploadJob : ResolvedUploadTarget
    {
        public ResolvedUploadJob(FileInfo sourceFile)
        {
            SourceFile = sourceFile;
        }

        public FileInfo SourceFile { get; }

        public string GetRelativePath()
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append('/');
            strBuilder.Append(UploadTargetConfig.RootPath?.Trim('/'));
            strBuilder.Append('/');
            if (UploadTarget.RelativeRootUploadPath != null)
            {
                strBuilder.Append(this.UploadTarget.RelativeRootUploadPath?.Trim('/'));
                strBuilder.Append('/');
            }
            var templateArr = StringUtils.DefaultTemplateVals.Append(('N',
                SourceFile.Directory?.Name ?? "Directory")).ToArray();
            var dirName = StringUtils.ApplyTemplatedFormattingString(
                UploadTarget.DirectoryNamingTemplate ?? throw new InvalidOperationException(), templateArr).Trim('/');
            strBuilder.Append(dirName);
     

            strBuilder.Append('/');
            
            string? fileName =
                StringUtils.ApplyTemplatedFormattingString(
                    UploadTarget.NamingTemplate ?? throw new ArgumentNullException(),
                    StringUtils.DefaultTemplateVals.Append(('N', SourceFile.Name.TrimEnd(SourceFile.Extension.ToCharArray()))).Append(('E',SourceFile.Extension.TrimStart('.'))).ToArray()).Trim('/');
            strBuilder.Append(fileName);


            return strBuilder.ToString();
        }
    }

    public static class StringUtils
    {
        private static readonly Regex _invalidFileNameChars = new("[\\\\/:\"*?<>|'`]+");

        public static (char, string)[] DefaultTemplateVals =
        {
            ('D', DateTime.Now.ToString("yyyy_MM_dd-HHmm"))
        };

        public static string ApplyTemplatedFormattingString(string templateString, params (char, string)[] strings)
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
            return _invalidFileNameChars.Replace(st, string.Empty).Replace(" ", "_");
        }
    }
}