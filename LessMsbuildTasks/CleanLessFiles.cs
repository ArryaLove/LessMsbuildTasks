using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace LessCompiler.Tasks
{
    public class CleanLessFiles : Task
    {
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        [Required]
        public string OutputFolder { get; set; }
        
        public bool KeepRelativeDirectory { get; set; }

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
                        outputFile = new FileInfo(Path.Combine(OutputFolder, item.GetMetadata("RelativeDir"), item.GetMetadata("FileName") + ".css"));
                    else
                        outputFile = new FileInfo(Path.Combine(OutputFolder, item.GetMetadata("Filename") + ".css"));

                    var shouldRun = true;

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
