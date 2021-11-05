namespace ZoomFileManager.Models.ConfigurationSchemas
{
    public class UploadTarget
    {
        public string? ConfigId { get; set; }
        public string? RelativeUploadPath { get; set; }
        public string? NamingTemplate { get; set; }
    }
}