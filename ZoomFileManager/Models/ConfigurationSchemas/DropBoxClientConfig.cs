namespace ZoomFileManager.Models.ConfigurationSchemas
{
    public class DropBoxClientConfig
    {
        public string? ApiKey { get; set; }
        public string? AppSecret { get; set; }
        public string? RefreshToken { get; set; }

        public string? AdminTeamMemberId { get; set; }
    }
}