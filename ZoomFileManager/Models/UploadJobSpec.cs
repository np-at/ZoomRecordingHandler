using System.IO;

namespace ZoomFileManager.Models
{
    public struct UploadJobSpec
    {
        public string TargetPath;

        public FileInfo FileInfo; 

        public JobType JobType;
        
        
    };
}