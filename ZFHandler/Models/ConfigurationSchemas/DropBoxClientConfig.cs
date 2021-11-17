namespace ZFHandler.Models.ConfigurationSchemas
{
    public class DropBoxClientConfig : BaseClientConfig
    {
        public string? ApiKey { get; set; }
        public string? AppSecret { get; set; }
        public string? RefreshToken { get; set; }

        public string? AdminTeamMemberId { get; set; }

        
    }
}