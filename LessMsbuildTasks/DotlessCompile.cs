using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using dotless.Core;
using dotless.Core.configuration;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace LessCompiler.Tasks
{
    public class DotlessCompile : Task
    {
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        [Required]
        public string OutputFolder { get; set; }

        public bool MinifyOutput { get; set; }

        public bool Debug { get; set; }

        public bool DisableUrlRewriting { get; set; }

        public bool ImportAllFilesAsLess { get; set; }

        public bool InlineCssFiles { get; set; }

        public bool DisableVariableRedefines { get; set; }

        public bool KeepFirstSpecialComment { get; set; }

        public bool ForceRun { get; set; }

        public bool KeepRelativeDirectory { get; set; }

        public Dictionary<string, bool> ProcessedFiles = new Dictionary<string, bool>();

        static Regex regex = new Regex(@"@import\s+""([^""]+)"";", RegexOptions.Compiled);


        public override bool Execute()
        {
            
            var config = new DotlessConfiguration
                {
                    ImportAllFilesAsLess = ImportAllFilesAsLess,
                    MinifyOutput = MinifyOutput,
                    Debug = Debug,
                    DisableUrlRewriting = DisableUrlRewriting,
                    DisableVariableRedefines =DisableVariableRedefines,
                    InlineCssFiles = InlineCssFiles,
                    KeepFirstSpecialComment = KeepFirstSpecialComment,
                };

            var engineFactory = new EngineFactory(config);
            var engine = engineFactory.GetEngine();
            var errorOccured = false;
            foreach (var item in InputFiles)
            {
                var currentDirectory = Environment.CurrentDirectory;
                try
                {
                    var inputFile = new FileInfo(item.GetMetadata("FullPath"));
                    FileInfo outputFile;
                    if (KeepRelativeDirectory)
                        outputFile = new FileInfo(Path.Combine(OutputFolder, item.GetMetadata("RelativeDir"), item.GetMetadata("FileName") + ".css"));
                    else
                        outputFile = new FileInfo(Path.Combine(OutputFolder, item.GetMetadata("Filename") + ".css"));

                    var shouldRun = true;

                    if (!ForceRun && outputFile.Exists)
                    {
                        shouldRun = Process(inputFile.FullName, outputFile.LastWriteTimeUtc);
                    }

                    if (shouldRun)
                    {
                        var text = File.ReadAllText(inputFile.FullName);
                        var directory = inputFile.DirectoryName;

                        Log.LogMessage(MessageImportance.High, "Compiling: {0} to {1}", item.ItemSpec, outputFile.FullName);
                        Environment.CurrentDirectory = directory;
                        var less = engine.TransformToCss(text, inputFile.FullName);

                        File.WriteAllText(outputFile.FullName, less);

                        Log.LogMessage(MessageImportance.High, "[Done]");

                        engine.ResetImports();
                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e);
                    errorOccured = true;
                }
                finally
                {
                    Environment.CurrentDirectory = currentDirectory;
                }
            }
                        
            return !errorOccured;
        }

        public bool Process(string filename, DateTime timeToCompare)
        {
            if (ProcessedFiles.ContainsKey(filename))
                return ProcessedFiles[filename];
            string file = File.ReadAllText(filename);
            string directory = directory = Path.GetDirectoryName(filename);
            var ret = File.GetLastWriteTimeUtc(filename) > timeToCompare;
            if (ret) return true;
            var matches = regex.Matches(file);
            foreach (Match match in matches)
            {
                var import = new FileInfo(Path.Combine(directory, match.Groups[1].Value));
                ret |= Process(import.FullName, timeToCompare);
                if (ret) break;
            }
            ProcessedFiles.Add(filename, ret);
            return ret;
        }
        
    }
}
