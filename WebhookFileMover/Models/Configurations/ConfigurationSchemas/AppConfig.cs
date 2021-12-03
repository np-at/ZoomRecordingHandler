using System;
using System.Collections.Generic;
using System.Net.Http;

namespace WebhookFileMover.Models.Configurations.ConfigurationSchemas
{
#if DEBUG
    using Newtonsoft.Json.Schema;
    using Newtonsoft.Json.Schema.Generation;
#endif

    public class AppConfig
    {
        public string[]? AllowedTokens { get; set; }
        public BaseReceiverConfig? BaseReceiverConfig { get; set; }
        public ReceiverEndpointConfig[]? ReceiverEndpointConfigs { get; set; }
        public UploadTargetConfig[]? UploadConfigs { get; set; }
        public UploadTarget[]? UploadTargets { get; set; }
        public SlackApiOptions? SlackApiOptions { get; set; }
        public NotificationOptions? NotificationOptions { get; set; }

        public AppConfig()
        {
#if DEBUG
            WriteSchema();
#endif
        }


        public AppConfig(string[] allowedTokens, UploadTargetConfig[] uploadConfigs, UploadTarget[] uploadTargets)
        {
            this.AllowedTokens = allowedTokens;
            UploadConfigs = uploadConfigs;
            UploadTargets = uploadTargets;

#if DEBUG
            WriteSchema();
#endif
        }
#if DEBUG
        public static void WriteSchema()
        {
            JSchemaGenerator generator = new();

            JSchema schema = generator.Generate(typeof(AppConfig));
            Console.WriteLine(schema.ToString(new JSchemaWriterSettings()
            {
                Version = SchemaVersion.Draft2019_09,
            }));
        }
#endif
        // public object GenerateRunConfigurations()
        // {
        //     foreach (UploadTarget uploadTarget in this.UploadTargets ?? ArraySegment<UploadTarget>.Empty)
        //     {
        //         var uploadConfig = this.UploadConfigs?.Where(x => x.Identifier?.Equals(uploadTarget.ConfigId, StringComparison.InvariantCultureIgnoreCase) ?? false) ?? throw new Exception(
        //             $"Upload Configuration with id {uploadTarget.ConfigId} not found for upload target {uploadTarget}");
        //         
        //     }
        // }
    }

    public enum AuthenticationMechanism
    {
        Unknown,
        ZoomBearer
    }

    public class BaseReceiverConfig
    {
        public string? BaseRouteTemplate { get; set; }
    }

    public class ReceiverEndpointConfig
    {
        public string? RouteSuffix { get; set; }
        public string? ModelTypeName { get; set; }
        public string[]? AssociatedUploadTargetIds { get; set; }
        public AuthenticationMechanism AuthenticationMechanism { get; set; }
        public IEnumerable<string>? AllowedAuthorizationHeaderValues { get; set; }

        public IEnumerable<NotificationProviderConfig>? NotificationProviderConfigs { get; set; }
        public Dictionary<string, string>? ValidationTests { get; set; }

    }

    public class NotificationProviderConfig
    {
        public NotificationProviderType ProviderType { get; set; }
        public string? Identifier { get; set; }
        public string? SuccessMessageTemplate { get; set; }
        public Dictionary<string, dynamic>? ParamBag { get; set; }
        public SlackApiOptions? SlackApiOptions { get; set; }

        public WebhookOptions? WebhookOptions { get; set; }
    }

    public class WebhookOptions
    {
        public string[]? Endpoints { get; set; }
        public string? BodyTemplate { get; set; }
        public HttpMethod? HttpMethod { get; set; }
        public Dictionary<string, string>? ExtraHeaders { get; set; }
    }

    public enum NotificationProviderType
    {
        Unknown,
        SlackBot,
        Webhook
    }

    public class UploadTaskDefinition
    {
    }
}