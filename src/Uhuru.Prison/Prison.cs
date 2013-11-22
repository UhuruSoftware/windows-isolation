using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Prison.Utilities.WindowsJobObjects;

namespace Uhuru.Prison
{
    public class Prison
    {
        static Type[] cellTypes = new Type[]{
            typeof(Restrictions.CPU),
            typeof(Restrictions.Disk),
            typeof(Restrictions.Filesystem),
            typeof(Restrictions.Firewall),
            typeof(Restrictions.Memory),
            typeof(Restrictions.Network),
            typeof(Restrictions.WindowStation)};

        List<Rule> prisonCells = new List<Rule>();
        JobObject jobObject = null;
        PrisonUser user = null;
        private static volatile bool wasInitialized = false;
        internal Native.STARTUPINFO ProcessStartupInfo = new Native.STARTUPINFO();

        public JobObject JobObject
        {
            get { return jobObject; }
        } 

        private bool isLocked = false;
        private PrisonRules prisonRules;
        private volatile bool used = false;


        public string Tag
        {
            get;
            set;
        }

        public PrisonRules Rules
        {
            get
            {
                return this.prisonRules;
            }
        }

        public PrisonUser User
        {
            get
            {
                return user;
            }
        }

        public Prison()
        {
        }

        private bool CellEnabled(RuleType cellTypeQuery)
        {
            return ((this.prisonRules.CellType & cellTypeQuery) == cellTypeQuery) || ((this.prisonRules.CellType & RuleType.All) == RuleType.All);
        }

        public void Lockdown(PrisonRules prisonRules)
        {
            if (this.isLocked)
            {
                throw new InvalidOperationException("This prison is already locked.");
            }

            this.prisonRules = prisonRules;

            if (prisonRules.CellType != RuleType.None)
            {
                foreach (Type cellType in cellTypes)
                {
                    Rule cell = (Rule)cellType.GetConstructor(Type.EmptyTypes).Invoke(null);
                    if (CellEnabled(cell.GetFlag()))
                    {
                        prisonCells.Add(cell);
                    }
                }
            }

            // Create the Windows User
            this.user = new PrisonUser(this.Tag);
            this.user.Create();

            // Create the JobObject
            this.jobObject = new JobObject(this.user.Username);
            this.jobObject.KillProcessesOnJobClose = true;

            // Lock all cells
            foreach (Rule cell in this.prisonCells)
            {
                cell.Apply(this);
            }

            this.isLocked = true;
        }

        public Process Execute(string filename)
        {
            return this.Execute(filename, string.Empty, false, null, new string[0]);
        }

        public Process Execute(string filename, string arguments)
        {
            return this.Execute(filename, arguments, false, null, new string[0]);
        }

        public Process Execute(string filename, string arguments, params string[] argumentsFormatParams)
        {
            return this.Execute(filename, arguments, false, null, argumentsFormatParams);
        }

        public Process Execute(string filename, string arguments, bool interactive, params string[] argumentsFormatParams)
        {
            return this.Execute(filename, arguments, interactive, null, argumentsFormatParams);
        }

        public Process Execute(string filename, string arguments, bool interactive, string env, params string[] argumentsFormatParams)
        {
            return this.Execute(filename, string.Format(arguments, argumentsFormatParams), interactive, env);
        }

        public Process Execute(string filename, string arguments, bool interactive, string env)
        {
            if (!this.isLocked)
            {
                throw new InvalidOperationException("This prison has to be locked before you can use it.");
            }

            if (this.used)
            {
                throw new InvalidOperationException("This prison has already been used to execute something.");
            }

            this.used = true;

            Native.PROCESS_INFORMATION processInfo = new Native.PROCESS_INFORMATION();

            Native.ProcessCreationFlags creationFlags = Native.ProcessCreationFlags.ZERO_FLAG;

            creationFlags &= ~Native.ProcessCreationFlags.CREATE_PRESERVE_CODE_AUTHZ_LEVEL;

            creationFlags |= Native.ProcessCreationFlags.CREATE_SEPARATE_WOW_VDM |
                Native.ProcessCreationFlags.CREATE_DEFAULT_ERROR_MODE |
                Native.ProcessCreationFlags.CREATE_NEW_PROCESS_GROUP |
                Native.ProcessCreationFlags.CREATE_SUSPENDED |
                Native.ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT;

            if (interactive)
            {
                creationFlags |= Native.ProcessCreationFlags.CREATE_NEW_CONSOLE;
            }
            else
            {
                creationFlags |= Native.ProcessCreationFlags.CREATE_NO_WINDOW;
            }

            bool startedOk = Native.CreateProcessWithLogonW(
                      this.user.Username, ".", this.user.Password,
                      Native.LogonFlags.LOGON_WITH_PROFILE,
                      null, string.Format("\"{0}\" {1}", filename, arguments), creationFlags, env,
                      this.prisonRules.PrisonHomePath, ref this.ProcessStartupInfo, out processInfo);

            if (!startedOk)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Process process = Process.GetProcessById(processInfo.dwProcessId);

            this.jobObject.AddProcess(process);
            
            uint resumeResult = Native.ResumeThread(processInfo.hThread);
            
            if (resumeResult != 1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Native.CloseHandle(processInfo.hProcess);
            Native.CloseHandle(processInfo.hThread);

            return process;
        }

        public void Destroy()
        {
        }

        public static void Init()
        {
            if (!Prison.wasInitialized)
            {
                Prison.wasInitialized = true;

                foreach (Type cellType in cellTypes)
                {
                    Rule cell = (Rule)cellType.GetConstructor(Type.EmptyTypes).Invoke(null);
                    cell.Init();
                }
            }
        }

        public static Dictionary<RuleType, RuleInstanceInfo[]> ListCellInstances()
        {
            Dictionary<RuleType, RuleInstanceInfo[]> result = new Dictionary<RuleType, RuleInstanceInfo[]>();

            foreach (Type cellType in cellTypes)
            {
                Rule cell = (Rule)cellType.GetConstructor(Type.EmptyTypes).Invoke(null);
                result[cell.GetFlag()] = cell.List();
            }

            return result;
        }
    }
}
