using System;
using System.Collections.Generic;
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
        string StdinPipeName { get; set; }

        [ComVisible(true)]
        string StdoutPipeName { get; set; }

        [ComVisible(true)]
        string StderrPipeName { get; set; }

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



        public string StdinPipeName { get; set; }

        public string StdoutPipeName { get; set; }

        public string StderrPipeName { get; set; }

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
            }
            else
            {
                this.StdinPipeName = null;
            }
            return this.StdinPipeName;
        }

        public string RedirectStdout(bool redirect)
        {
            if (redirect)
            {
                this.StdoutPipeName = runId + "\\stdout";
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
            }
            else
            {
                this.StderrPipeName = null;
            }
            return this.StderrPipeName;
        }

    }
}
