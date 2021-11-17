using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace ZFHandler.Helpers
{
    public static class FileHelpers
    {
        public static async Task<bool> IsFileLocked(IFileInfo file)
        {
            try
            {
                await using FileStream stream = File.Open(file.PhysicalPath, FileMode.Open, FileAccess.Read,
                    FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }


            //file is not locked
            return false;
        }
        
    }
}