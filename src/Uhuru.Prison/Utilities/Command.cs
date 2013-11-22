using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Uhuru.Prison.Utilities
{
    /// <summary>
    /// Callback for the process stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    public delegate void StreamWriterCallback(StreamWriter stream);

    /// <summary>
    /// The callback that is executed after a process stopped.
    /// </summary>
    /// <param name="output">The output stream.</param>
    /// <param name="statusCode">The status code.</param>
    public delegate void ProcessDoneCallback(string output, int statusCode);

    public class Command
    {
        /// <summary>
        /// Starts up a new process and executes a command.
        /// </summary>
        /// <param name="command"> The command to execute. </param>
        /// <param name="arguments"> The arguments of the command. </param>
        /// <returns>The output of the executed command.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Suitable fur the current context.")]
        public static string RunCommandAndGetOutput(string command, string arguments)
        {
            return RunCommandAndGetOutput(command, arguments, false);
        }

        /// <summary>
        /// Starts up a new process and executes a command.
        /// </summary>
        /// <param name="command"> The command to execute. </param>
        /// <param name="arguments"> The arguments of the command. </param>
        /// <returns>The output of the executed command, including errors.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Suitable fur the current context.")]
        public static string RunCommandAndGetOutputAndErrors(string command, string arguments)
        {
            return RunCommandAndGetOutput(command, arguments, true);
        }

        /// <summary>
        /// Starts up a new process and executes a command.
        /// </summary>
        /// <param name="command"> The command to execute. </param>
        /// <param name="arguments"> The arguments of the command. </param>
        /// <param name="outputIncludesErrors"> A value indicated whether the errors are to be included in output or not. </param>
        /// <returns>The output of the executed command.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "More suitable for the current situation.")]
        private static string RunCommandAndGetOutput(string command, string arguments, bool outputIncludesErrors)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = command;
            start.Arguments = arguments;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            using (Process process = Process.Start(start))
            {
                string result = process.StandardOutput.ReadToEnd();

                if (outputIncludesErrors)
                {
                    result += process.StandardError.ReadToEnd();
                }

                return result;
            }
        }

        /// <summary>
        /// starts a new process and executes a command
        /// </summary>
        /// <param name="shell">The command to be executed.</param>
        /// <param name="arguments">The arguments of the command.</param>
        /// <param name="writerCallback">The callback to process the input.</param>
        /// <param name="doneCallback">The callback to process the output.</param>
        public static void ExecuteCommands(string shell, string arguments, StreamWriterCallback writerCallback, ProcessDoneCallback doneCallback)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object data)
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = shell;
                start.Arguments = arguments;
                start.CreateNoWindow = true;
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardInput = true;

                string result = string.Empty;

                using (Process process = Process.Start(start))
                {
                    using (StreamWriter writer = process.StandardInput)
                    {
                        writerCallback(writer);
                    }

                    using (StreamReader reader = process.StandardOutput)
                    {
                        result = reader.ReadToEnd();
                    }

                    process.WaitForExit();
                    doneCallback(result, process.ExitCode);
                }
            }));
        }

        /// <summary>
        /// Starts up a cmd shell and executes a command.
        /// </summary>
        /// <param name="command">The command to be executed.</param>
        /// <returns>The process' exit code.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Suitable fur the current context.")]
        public static int ExecuteCommand(string command)
        {
            ProcessStartInfo pi = new ProcessStartInfo("cmd", "/c " + command);
            pi.CreateNoWindow = true;
            pi.UseShellExecute = false;
            Process p = Process.Start(pi);
            p.WaitForExit();
            return p.ExitCode;
        }

        /// <summary>
        /// Starts up a cmd shell and executes a command.
        /// </summary>
        /// <param name="command">The command to be executed.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>The process' exit code.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Suitable fur the current context.")]
        public static int ExecuteCommand(string command, string workingDirectory)
        {
            ProcessStartInfo pi = new ProcessStartInfo("cmd", "/c " + command);
            pi.CreateNoWindow = true;
            pi.UseShellExecute = false;
            pi.WorkingDirectory = workingDirectory;
            Process p = Process.Start(pi);
            p.WaitForExit();
            return p.ExitCode;
        }

        /// <summary>
        /// Starts up a cmd shell and executes a command.
        /// </summary>
        /// <param name="command">The command to be executed.</param>
        /// <param name="timeout">Seconds to wait before killing the process.</param>
        /// <returns>The process' exit code.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Suitable fur the current context.")]
        public static int ExecuteCommand(string command, int timeout)
        {
            ProcessStartInfo pi = new ProcessStartInfo("cmd", "/c " + command);
            pi.CreateNoWindow = true;
            pi.UseShellExecute = false;
            Process p = Process.Start(pi);
            p.WaitForExit((int)TimeSpan.FromSeconds(timeout).TotalMilliseconds);
            if (!p.HasExited)
            {
                p.Kill();
            }
            return p.ExitCode;
        }
    }
}
