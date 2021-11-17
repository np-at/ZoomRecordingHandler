using System;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace ZFHandler.Models.ConfigurationSchemas
{
#if DEBUG
#endif

    public class AppConfig
    {
        public string[]? AllowedTokens { get; set; }
        public ReceiverConfig[]? ReceiverConfigs { get; set; }
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
            Console.WriteLine(schema.ToString(SchemaVersion.Draft2019_09));
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

    public enum ReceiverType
    {
        Unknown,
        ZoomWebhook
    }

    public enum AuthenticationMechanism
    {
        Unknown,
        ZoomBearer
    }
    public class ReceiverConfig
    {
        public ReceiverType ReceiverType { get; set; }
        
        public AuthenticationMechanism AuthenticationMechanism { get; set; }
    }

    public class NotificationProviderConfig
    {
        public NotificationProviderType ProviderType { get; set; }
        
    }

    public enum NotificationProviderType
    {
        Unknown,
        SlackWebhook
    }

    public class UploadTaskDefinition
    {
    }
 
}