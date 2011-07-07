using System;
using System.Linq;
using com.google.javascript.jscomp;
using Microsoft.Build.Framework;

namespace ClosureCompilerMsBuild
{
    public class ClosureLocalCompiler : ClosureCompilerBase
    {

        public override bool Execute()
        {
            if (!ValidateCompilationLevel())
                return false;

            var errorCount = 0;
            var options = new CompilerOptions();
            CompilationLevel compilationLevel = null;
            switch (CompilationLevel)
            {
                case "WHITESPACE_ONLY":
                    compilationLevel = com.google.javascript.jscomp.CompilationLevel.WHITESPACE_ONLY;
                    break;
                case "SIMPLE_OPTIMIZATIONS":
                    compilationLevel = com.google.javascript.jscomp.CompilationLevel.SIMPLE_OPTIMIZATIONS;
                    break;
                case "ADVANCED_OPTIMIZATIONS":
                    compilationLevel = com.google.javascript.jscomp.CompilationLevel.ADVANCED_OPTIMIZATIONS;
                    break;
            }
            compilationLevel.setOptionsForCompilationLevel(options);

            Log.LogMessage(String.Format("Found {0} javascript files to compile using the Closure compiler", SourceFiles.Count()));
            foreach (var t in SourceFiles)
            {
                Log.LogMessage(String.Concat("Compiling JS file: ", t.ItemSpec), null);
                var compiler = new Compiler();
                var dummy = JSSourceFile.fromCode("externs.js", "");
                var source = JSSourceFile.fromFile(t.ItemSpec);
                var thisErrorCount = 0;

                var result = compiler.compile(dummy, source, options);

                if (!result.success && IsShowErrors)
                {
                    foreach (var error in result.errors)
                    {
                        LogError(t.ItemSpec, error.lineNumber, error.getCharno(), error.description);
                        errorCount++;
                        thisErrorCount++;
                    }
                }
                if (IsShowWarnings)
                {
                    foreach (var warning in result.warnings)
                    {
                        LogWarning(t.ItemSpec, warning.lineNumber, warning.getCharno(), warning.description);
                    }
                }

                if (!MaybeWriteOutput(t.ItemSpec, compiler.toSource()))
                    return false;
            }
            return errorCount == 0;
        }

    }
}