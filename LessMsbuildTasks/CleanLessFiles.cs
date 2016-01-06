using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace LessMsbuildTasks
{
    public class CleanLessFiles : Task
    {
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        [Required]
        public string OutputFolder { get; set; }

        public bool KeepRelativeDirectory { get; set; }

        public string Extension { get; set; }

        public override bool Execute()
        {
            var errorOccured = false;
            foreach (var item in InputFiles)
            {
                try
                {
                    var inputFile = new FileInfo(item.GetMetadata("FullPath"));
                    FileInfo outputFile;
                    if (KeepRelativeDirectory)
                        outputFile = new FileInfo(Path.Combine(OutputFolder, item.GetMetadata("RelativeDir"), item.GetMetadata("FileName") + Extension));
                    else
                        outputFile = new FileInfo(Path.Combine(OutputFolder, item.GetMetadata("Filename") + Extension));

                    if (outputFile.Exists)
                    {
                        outputFile.Delete();
                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e);
                    errorOccured = true;
                }
            }
            return !errorOccured;
        }
    }
}
