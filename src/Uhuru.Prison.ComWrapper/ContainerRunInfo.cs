using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.ComWrapper
{
    [ComVisible(true)]
    public interface IContainerRunInfo
    {
        [ComVisible(true)]
        string Filename { get; set; }

        [ComVisible(true)]
        string Arguments { get; set; }

        [ComVisible(true)]
        string StdinPipeName { get; }
        PipeStream StdinPipe { get; }

        [ComVisible(true)]
        string StdoutPipeName { get;  }
        PipeStream StdoutPipe { get; }

        [ComVisible(true)]
        string StderrPipeName { get;  }
        PipeStream StderrPipe { get; }

        [ComVisible(true)]
        Dictionary<string, string> ExtraEnvironmentVariables { get; }

        [ComVisible(true)]
        void AddEnvironemntVariable(string key, string value);

        [ComVisible(true)]
        void RemoveEnvironemntVariable(string key);

        [ComVisible(true)]
        string RedirectStdin(bool redirect);

        [ComVisible(true)]
        string RedirectStdout(bool redirect);

        [ComVisible(true)]
        string RedirectStderr(bool redirect);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class ContainerRunInfo : IContainerRunInfo
    {
        private string runId;

        public string Filename { get; set; }

        public string Arguments { get; set; }



        public string StdinPipeName { get; private set; }
        public PipeStream StdinPipe { get; private set; }

        public string StdoutPipeName { get; private set; }
        public PipeStream StdoutPipe { get; private set; }

        public string StderrPipeName { get; private set; }
        public PipeStream StderrPipe { get; private set; }

        public Dictionary<string, string> ExtraEnvironmentVariables { get; private set; }

        public void AddEnvironemntVariable(string key, string value)
        {
            ExtraEnvironmentVariables[key] = value;
        }

        public void RemoveEnvironemntVariable(string key)
        {
            ExtraEnvironmentVariables.Remove(key);
        }

        public ContainerRunInfo()
        {
            this.runId = Guid.NewGuid().ToString();
            this.ExtraEnvironmentVariables = new Dictionary<string, string>();
        }

        public string RedirectStdin(bool redirect)
        {
            if (redirect)
            {
                this.StdinPipeName = runId + "\\stdin";
                if (this.StdinPipe == null)
                {
                    this.StdinPipe = new NamedPipeServerStream(this.StdinPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 1024 * 128, 1024 * 128, null, HandleInheritability.Inheritable);
                }
            }
            else
            {
                this.StdinPipeName = null;
                if (this.StdinPipe != null)
                {
                    this.StdinPipe.Dispose();
                    this.StdinPipe = null;
                }
            }
            return this.StdinPipeName;
        }

        public string RedirectStdout(bool redirect)
        {
            if (redirect)
            {
                this.StdoutPipeName = runId + "\\stdout";
                if (this.StdoutPipe == null)
                {
                    this.StdoutPipe = new NamedPipeServerStream(this.StdoutPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 1024 * 128, 1024 * 128, null, HandleInheritability.Inheritable);
                }
            }
            else
            {
                this.StdoutPipeName = null;
            }
            return this.StdoutPipeName;
        }

        public string RedirectStderr(bool redirect)
        {
            if (redirect)
            {
                this.StderrPipeName = runId + "\\stderr";
                if (this.StderrPipe == null)
                {
                    this.StderrPipe = new NamedPipeServerStream(this.StderrPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 1024 * 128, 1024 * 128, null, HandleInheritability.Inheritable);
                }
            }
            else
            {
                this.StderrPipeName = null;
            }
            return this.StderrPipeName;
        }

    }
}
