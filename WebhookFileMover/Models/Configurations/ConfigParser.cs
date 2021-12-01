using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WebhookFileMover.Controllers;
using WebhookFileMover.Models.Configurations.ConfigurationSchemas;
using WebhookFileMover.Models.Configurations.Internal;

namespace WebhookFileMover.Models.Configurations
{
    public class JobConfigWrapper
    {
        public Dictionary<string, ResolvedUploadTarget>? Targets { get; set; }
       
    }
    public static class ConfigParser
    {
        public static IEnumerable<ResolvedUploadTarget> AddWFConfiguration(this IServiceCollection services,
            AppConfig appConfig)
        {
            
            var targets = ResolveTargets(appConfig);
            foreach (var resolvedUploadTarget in targets)
            {
                services.Configure<ResolvedUploadTarget>(resolvedUploadTarget.Id, target =>
                {
                    target.Id = resolvedUploadTarget.Id;
                    target.UploadTarget = resolvedUploadTarget.UploadTarget;
                    target.UploadTargetConfig = resolvedUploadTarget.UploadTargetConfig;

                });
            }
            services.Configure<JobConfigWrapper>(wrapper => wrapper.Targets = targets.ToDictionary(x => x.Id));
            return targets;
        }
        private static IEnumerable<ResolvedUploadTarget> ResolveTargets(AppConfig appConfig)
        {
            List<ResolvedUploadTarget> resolvedUploadTargets = new();
            var uploadTargets = appConfig.UploadTargets ??
                                throw new ArgumentNullException(nameof(appConfig.UploadTargets));
            var uploadConfigs = appConfig.UploadConfigs ??
                                throw new ArgumentNullException(nameof(appConfig.UploadConfigs));
            foreach (var uploadTarget in uploadTargets)
            {
                var uploadConfig = uploadConfigs.Where(x => x.Identifier == uploadTarget.ConfigId).ToImmutableArray();
                if (uploadConfig.Length is 0 or > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(uploadTarget.ConfigId),
                        "Require exactly 1 corresponding config for provided ConfigId value in Upload Target");
                }

                var newResolvedTarget = new ResolvedUploadTarget()
                {
                    Id = Guid.NewGuid().ToString("N"),
                    UploadTarget = uploadTarget,
                    UploadTargetConfig = uploadConfig[0]
                };
                resolvedUploadTargets.Add(newResolvedTarget);

            }

            return resolvedUploadTargets;
        }
    }
}