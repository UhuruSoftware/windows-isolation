using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Tests
{
    static class Utilities
    {
        public static string CreateFileOfSize(int sizeMB)
        {
            string filename = Path.GetTempFileName();

            for (int size = 1; size < sizeMB; size++)
            {
                byte[] content = new byte[1024 * 1024];

                File.AppendAllText(filename, ASCIIEncoding.ASCII.GetString(content));
            }

            File.GetAccessControl(filename).SetAccessRule(
                new System.Security.AccessControl.FileSystemAccessRule(
                    "Everyone", System.Security.AccessControl.FileSystemRights.Read, System.Security.AccessControl.AccessControlType.Allow));

            return filename;
        }

        public static string CreateExeForPrison(string code, Prison prison)
        {
            string filename = Path.GetTempFileName() + ".exe";

            Dictionary<string, string> providerOptions = new Dictionary<string, string>
                {
                    {"CompilerVersion", "v3.5"}
                };

            code = string.Format(@"
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Runtime.ConstrainedExecution;

class Program
{{

[DllImport(""kernel32.dll"", SetLastError = true)]
static extern int SetErrorMode(int wMode);

[DllImport(""kernel32.dll"")]
static extern FilterDelegate SetUnhandledExceptionFilter(FilterDelegate lpTopLevelExceptionFilter);
public delegate bool FilterDelegate(Exception ex);

public static void DisableCrashReport()
{{
 FilterDelegate fd = delegate(Exception ex)
 {{
  return true;
 }};
 SetUnhandledExceptionFilter(fd);
 SetErrorMode(SetErrorMode(0) | 0x0002 );
}}

static int Main(string[] args)
{{
DisableCrashReport();
{0}

return 0;
}}
}}", code);

            CSharpCodeProvider codeProvider = new CSharpCodeProvider(providerOptions);
    

            System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = filename;
            parameters.ReferencedAssemblies.AddRange(new string[] { "System.dll", "System.Net.dll" });
            
            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, code);

            StringBuilder errors = new StringBuilder();
            bool hasError = false;

            foreach (CompilerError error in results.Errors)
            {
                errors.AppendLine(error.ErrorText);
                hasError = hasError || (!error.IsWarning);
            }

            if (hasError)
            {
                throw new InvalidOperationException(errors.ToString());
            }

            File.GetAccessControl(filename).SetAccessRule(
                new System.Security.AccessControl.FileSystemAccessRule(
                    prison.User.Username, System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));

            string outFile = Path.Combine(prison.Rules.PrisonHomePath, Path.GetFileName(filename));
            File.Copy(filename, outFile);

            return outFile;
        }
    }
}
