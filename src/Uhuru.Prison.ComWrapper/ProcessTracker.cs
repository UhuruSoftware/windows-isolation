using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.ComWrapper
{
    [ComVisible(true)]
    public interface IProcessTracker
    {
        [ComVisible(true)]
        int GetPid();

        [ComVisible(true)]
        int GetExitCode();

        [ComVisible(true)]
        bool HasExited();

        [ComVisible(true)]
        void Wait();
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class ProcessTracker : IProcessTracker
    {
        private System.Diagnostics.Process sysProc;

        public int GetPid()
        {
            return this.sysProc.Id;
        }
        public int GetExitCode()
        {
                return this.sysProc.ExitCode;
        }

        public bool HasExited()
        {
            return this.sysProc.HasExited;
        }

        public void Wait()
        {
            this.sysProc.WaitForExit();
        }

        public ProcessTracker(System.Diagnostics.Process p)
        {
            this.sysProc = p;
        }

    }
}
