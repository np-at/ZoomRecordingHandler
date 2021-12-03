namespace WebhookFileMover.Database.Models
{
    public enum JobTaskType
    {
        Unknown,
        Download,
        UploadNOS,
        UploadSharepoint,
        UploadOnedriveDrive,
        UploadOnedriveUser,
        UploadDropbox
    }
}