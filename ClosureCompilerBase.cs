using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ClosureCompilerMsBuild
{
    public abstract class ClosureCompilerBase : Task
    {
        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        public String SourceExtensionPattern { get; set; }

        public String TargetExtension { get; set; }
        public String TargetDirectory { get; set; }

        public bool IsWriteOutput { get; set; }
        public bool IsShowWarnings { get; set; }
        public bool IsShowErrors { get; set; }
        public bool IntegrateWithVisualStudio { get; set; }

        private String _compilationLevel = "WHITESPACE_ONLY";
        public String CompilationLevel
        {
            get { return _compilationLevel; }
            set { _compilationLevel = value; }
        }

        protected bool MaybeWriteOutput(string originalFilename, string content)
        {
            if (IsWriteOutput)
            {
                bool writeAlongside = false;
                if (string.IsNullOrEmpty(TargetDirectory))
                    writeAlongside = true;
                else if (!Directory.Exists(TargetDirectory))
                {
                    Log.LogError("The target folder '" + TargetDirectory + "' doesn't exist!");
                    return false;
                }
                var targetFile = Regex.Replace(originalFilename, SourceExtensionPattern, TargetExtension);
                if (!writeAlongside)
                    targetFile = Path.Combine(TargetDirectory, new FileInfo(targetFile).Name);
                File.WriteAllText(targetFile, content, Encoding.ASCII);
            }
            return true;
        }

        protected bool ValidateCompilationLevel()
        {
            if (!new[] { "WHITESPACE_ONLY", "SIMPLE_OPTIMIZATIONS", "ADVANCED_OPTIMIZATIONS" }.Contains(CompilationLevel))
            {
                Log.LogError("Unknows compilation level '" + CompilationLevel + " - valid values are WHITESPACE_ONLY, SIMPLE_OPTIMIZATIONS, ADVANCED_OPTIMIZATIONS");
                return false;
            }
            return true;
        }

        protected void LogError(string filename, int line, int charNo, string message)
        {
            Log.LogError("[closure-compiler]", "", null, filename, line,charNo, line, charNo, message);
        }

        protected void LogWarning(string filename, int line, int charNo, string message)
        {
            Log.LogWarning("[closure-compiler]", "", null, filename, line, charNo, line, charNo, message);
        }
    }
}