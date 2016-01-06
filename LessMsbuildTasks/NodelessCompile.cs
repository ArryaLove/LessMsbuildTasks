using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace LessMsbuildTasks
{
    public class NodelessCompile : Task
    {
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        /// <summary>
        /// Folder to output .css file
        /// </summary>
        [Required]
        public string OutputFolder { get; set; }

        /// <summary>
        /// Adds --compress
        /// </summary>
        public bool MinifyOutput { get; set; }

		    /// <summary>
		    /// If true, source maps are created
		    /// </summary>
		    public bool CreateSourceMap { get; set; }

        /// <summary>
        /// Adds --line-numbers=comments
        /// This will output filename and line number references to all css
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Add --verbose
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Add --strict-imports
        /// Causes all imports to be parsed.  The default is to just parse .less imports.
        /// </summary>
        public bool ImportAllFilesAsLess { get; set; }

        /// <summary>
        /// The extension to convert the files to.
        /// </summary>
        public string Extension{ get; set; }

        /// <summary>
        /// Path to Less folder. This folder should have a bin/lessc file.
        /// </summary>
        public string LessPath { get; set; }

        /// <summary>
        /// If this is true it will place the generated css files in the same relative file structure as the original files (relative to the project root)
        /// </summary>
        public bool KeepRelativeDirectory { get; set; }

        /// <summary>
        /// Add --relative-urls
        /// Makes all of the urls in the .less file relative.
        /// </summary>
        public bool LessDisableUrlRewriting { get; set; }

		/// <summary>
		/// Set global variables for the less compilation defined in an item group array
		/// Format for each item "my-var=value"
		/// </summary>
		public string[] LessGlobalVars { get; set; }

		/// <summary>
		/// Modify variables for the less compilation defined in an item group array
		/// Format for each item "my-var=value"
		/// </summary>
		public string[] LessModifyVars { get; set; }

        /// <summary>
        /// Maintain a list of processed files so we don't process them again.
        /// </summary>
        private readonly Dictionary<string, bool> _ProcessedFiles = new Dictionary<string, bool>();

        /// <summary>
        /// Regex to look for import statements in each file.
        /// </summary>
        private static readonly Regex _RegexImport = new Regex(@"@import\s+""([^""]+)"";", RegexOptions.Compiled);

        /// <summary>
        /// Maintain a hash of errors so we don't repeat the same ones.
        /// </summary>
        private HashSet<string> _Errors;


        public override bool Execute()
        {

            _Errors = new HashSet<string>();

            var success = true;
            foreach (var item in InputFiles)
            {
                try {
                    var inputFile = new FileInfo(item.GetMetadata("FullPath"));
                    FileInfo outputFile;
                    
                    if (KeepRelativeDirectory)
                        outputFile = new FileInfo(Path.Combine(OutputFolder, item.GetMetadata("RelativeDir"), item.GetMetadata("FileName") + Extension));
                    else
                        outputFile = new FileInfo(Path.Combine(OutputFolder, item.GetMetadata("Filename") + Extension));

                    var shouldRun = true;

                    if (outputFile.Exists)
                    {
                        shouldRun = Process(inputFile.FullName, outputFile.LastWriteTimeUtc);
                    }

                    if (shouldRun)
                    {
                        var directory = inputFile.DirectoryName;

                        Log.LogMessage(MessageImportance.High, "Compiling: {0} to {1}", inputFile.FullName, outputFile.FullName);

                        success = success && CompileLessFile(inputFile.FullName, outputFile.FullName , directory);

                        Log.LogMessage(MessageImportance.High, "[Done]");

                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e);
                    success = false;
                }
            }

            return success;
        }

        private string GetAppPath()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string assemblyLocation = assembly.Location;
            var fInfo = new FileInfo(assemblyLocation);
            var fdir = fInfo.Directory;
            System.Diagnostics.Debug.Assert( fdir != null, "AppPath != null" );
            return fdir.FullName;
        }

        private string Quote( string text )
        {
            return "\"" + text + "\"";
         }

        private string GetLesscArguments(string inputFilePath, string outputFilePath)
        {
            var appPath = String.IsNullOrEmpty(LessPath) ? GetAppPath() : LessPath;
            var lesscPath =  Path.Combine(appPath,"less", "bin", "lessc");

            var args = new List<String>();

            //javascript to run from node (lessc)
            args.Add( Quote(lesscPath) );
            
            //eliminate color tags for output
            args.Add( "--no-color" );

            if (Verbose)
              args.Add( "--verbose" );

            //Debug mode
            if (Debug)
                args.Add( "--line-numbers=comments" );

            //Compress CSS
            if (MinifyOutput)
              args.Add("--clean-css");

					if(CreateSourceMap)
						args.Add(string.Format("--source-map={0}.map", outputFilePath));

            //Force all imports to be compiled
            if (ImportAllFilesAsLess)
                args.Add( "--strict-imports" );

            //Add this option to make urls relative.
            if (!LessDisableUrlRewriting)
                args.Add( "--relative-urls" );

            //Add input file
            args.Add( Quote(inputFilePath) );

            //Add output file
            args.Add( Quote(outputFilePath) );

			if ( this.LessGlobalVars != null ) {
				foreach ( var globalVar in this.LessGlobalVars ) {
					args.Add( "--global-var=\"" + globalVar + "\"" );
				}
			}

			if ( this.LessModifyVars != null ) {
				foreach ( var modVar in this.LessModifyVars ) {
					args.Add( "--modify-var=\"" + modVar + "\"" );
				}
			}

            return String.Join( " ", args );

        }

        private bool CompileLessFile( string inputFilePath, string outputFilePath, string workingDirectory )
        {

            var result = true;

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(GetAppPath(), "node"),
                Arguments = GetLesscArguments(inputFilePath,outputFilePath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            var output = new StringBuilder();
            var error = new StringBuilder();
            var proc = new Process { StartInfo = psi };

            
            proc.OutputDataReceived += ( sender, args ) => output.AppendLine( args.Data );
            
            proc.ErrorDataReceived += ( sender, args ) => error.AppendLine( args.Data );
            
            proc.Start();

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            if (!proc.HasExited)
                proc.WaitForExit();

            var outputStr = output.ToString();
            if (outputStr.Trim().Length > 0)
                Log.LogMessage( MessageImportance.High, outputStr);

            var errorStr = error.ToString();
            if (errorStr.Trim().Length > 0)
            {
                if (!_Errors.Contains( errorStr ))
                {
                    result = false;
                    Log.LogError( errorStr );
                    _Errors.Add( errorStr );
                }
            }

            return result;


        }

        private bool Process(string filename, DateTime timeToCompare)
        {
            if (_ProcessedFiles.ContainsKey(filename))
                return _ProcessedFiles[filename];
            string file = File.ReadAllText(filename);
            string directory = Path.GetDirectoryName(filename);
            var ret = File.GetLastWriteTimeUtc(filename) > timeToCompare;
            if (ret) return true;
            var matches = _RegexImport.Matches(file);
            foreach (Match match in matches)
            {
                var import = new FileInfo(Path.Combine(directory, match.Groups[1].Value));
                ret |= Process(import.FullName, timeToCompare);
                if (ret) break;
            }
            _ProcessedFiles.Add(filename, ret);
            return ret;
        }

    }
}
