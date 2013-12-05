using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uhuru.Prison.Utilities.WindowsJobObjects;

namespace Uhuru.Prison
{
    [DataContract]
    public class Prison
    {
        static Type[] cellTypes = new Type[]{
            typeof(Restrictions.CPU),
            typeof(Restrictions.Disk),
            typeof(Restrictions.Filesystem),
            typeof(Restrictions.Httpsys),
            typeof(Restrictions.Memory),
            typeof(Restrictions.Network),
            typeof(Restrictions.WindowStation)};

        List<Rule> prisonCells = new List<Rule>();

        JobObject jobObject = null;

        PrisonUser user = null;
        private static volatile bool wasInitialized = false;

        internal Native.STARTUPINFO ProcessStartupInfo = new Native.STARTUPINFO();

        private const string databaseLocation = @".\db";

        public JobObject JobObject
        {
            get { return jobObject; }
        }

        private bool isLocked = false;
        private PrisonRules prisonRules;

        [DataMember]
        public Guid ID
        {
            get;
            set;
        }

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
            this.ID = Guid.NewGuid();
            this.Save();
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

            if (this.prisonRules.TotalPrivateMemoryLimitBytes > 0)
            {
                RunGuard();
            }

            this.isLocked = true;
        }

        private Process RunGuard()
        {
            var psi = new ProcessStartInfo();
            psi.UseShellExecute = false;
            psi.ErrorDialog = false;
            psi.CreateNoWindow = true;

            // TODO: rename TotalPrivateMemoryLimitBytes to a more general term
            psi.FileName = GetGuardPath();
            psi.Arguments = this.user.Username + " " + this.prisonRules.TotalPrivateMemoryLimitBytes;

            return Process.Start(psi);
        }

        private void TryStopGuard()
        {
            EventWaitHandle dischargeEvent = null;
            EventWaitHandle.TryOpenExisting("discharge-" + this.user.Username, out dischargeEvent);

            if (dischargeEvent != null)
            {
                dischargeEvent.Set();
            }
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

            var startupInfo = new Native.STARTUPINFO();
            var processInfo = new Native.PROCESS_INFORMATION();

            startupInfo = this.ProcessStartupInfo;

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
                startupInfo.lpDesktop = "";
            }
            else
            {
                creationFlags |= Native.ProcessCreationFlags.CREATE_NO_WINDOW;
            }
            
            var delegateStartInfo = new ProcessStartInfo();
            delegateStartInfo.FileName = GetCreateProcessDeletegatePath();
            delegateStartInfo.UseShellExecute = false;

            if (interactive)
            {
                delegateStartInfo.CreateNoWindow = false;
                delegateStartInfo.ErrorDialog = true;
            }
            else
            {
                delegateStartInfo.CreateNoWindow = true;
                delegateStartInfo.ErrorDialog = false;
            }

            delegateStartInfo.RedirectStandardInput = true;
            delegateStartInfo.RedirectStandardOutput = true;
            
            delegateStartInfo.EnvironmentVariables["Method"] = "CreateProcessWithLogonW";
            delegateStartInfo.EnvironmentVariables["pUsername"] = this.user.Username;
            delegateStartInfo.EnvironmentVariables["Domain"] = ".";
            delegateStartInfo.EnvironmentVariables["Password"] = this.user.Password;
            delegateStartInfo.EnvironmentVariables["LogonFlags"] = ((int)Native.LogonFlags.LOGON_WITH_PROFILE).ToString();
            delegateStartInfo.EnvironmentVariables["CommandLine"] = string.IsNullOrWhiteSpace(filename) ? arguments : '"' + filename + "\" " + arguments;
            delegateStartInfo.EnvironmentVariables["CreationFlags"] = ((int)(creationFlags)).ToString();
            delegateStartInfo.EnvironmentVariables["CurrentDirectory"] = this.prisonRules.PrisonHomePath;
            delegateStartInfo.EnvironmentVariables["Desktop"] = startupInfo.lpDesktop;

            var delegateProcess = Process.Start(delegateStartInfo);

            // Delegate Process: started in suspended state
            // Working  Process: not started

            // Take the process with the Job Object before resuming the process.
            this.jobObject.AddProcess(delegateProcess);

            delegateProcess.StandardInput.WriteLine("CreateProcess");

            // Wait for response
            var workerProcessPid = int.Parse(delegateProcess.StandardOutput.ReadLine());
            var workerProcess = Process.GetProcessById(workerProcessPid);

            // This would allow the process to query the ExitCode. ref: http://msdn.microsoft.com/en-us/magazine/cc163900.aspx
            workerProcess.EnableRaisingEvents = true;

            // Delegate Process: ending
            // Working  Process: started in suspended state

            delegateProcess.WaitForExit();

            if (delegateProcess.ExitCode != 0)
            {
                throw new Win32Exception(delegateProcess.ExitCode);
            }

            // Delegate Process: finished
            // Working  Process: started in suspended state

            // Now that the process is is gated with the Job Object so we can resume the thread.
            IntPtr threadHandler = Native.OpenThread(Native.ThreadAccess.SUSPEND_RESUME, false, workerProcess.Threads[0].Id);

            uint resumeResult = Native.ResumeThread(threadHandler);

            Native.CloseHandle(threadHandler);

            if (resumeResult != 1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Delegate Process: finished
            // Working  Process: running

            return workerProcess;

            // Other IPC methods with the delegate.
            // http://stackoverflow.com/questions/16129113/passing-c-char-to-c-sharp-via-shared-memory
            // or http://stackoverflow.com/questions/2640642/c-implementing-named-pipes-using-the-win32-api and
            // http://msdn.microsoft.com/en-us/library/system.io.pipes.namedpipeserverstream(v=vs.110).aspx
        }

        public void Destroy()
        {
            TryStopGuard();
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

        private void Save()
        {
            string assemblyLocation = Path.GetDirectoryName(typeof(Prison).Assembly.Location);
            string dbDirectory = Path.Combine(assemblyLocation, Prison.databaseLocation);

            Directory.CreateDirectory(dbDirectory);

            string prisonFile = Path.GetFullPath(Path.Combine(dbDirectory, string.Format("{0}.xml", this.ID.ToString("N"))));

            DataContractSerializer serializer = new DataContractSerializer(typeof(Prison));

            using (FileStream writeStream = File.Open(prisonFile, FileMode.Create, FileAccess.Write))
            {
                serializer.WriteObject(writeStream, this);
            }
        }

        /// <summary>
        /// Loads all persisted Prison instances.
        /// <remarks>
        /// This method assumes that serialized Prison objects are stored in a folder named 'db', next to the assembly.
        /// </remarks>
        /// </summary>
        /// <returns>An array of Prison objects.</returns>
        public static Prison[] Load()
        {
            List<Prison> result = new List<Prison>();

            string assemblyLocation = Path.GetDirectoryName(typeof(Prison).Assembly.Location);
            string loadLocation = Path.GetFullPath(Path.Combine(assemblyLocation, Prison.databaseLocation));

            Directory.CreateDirectory(loadLocation);

            string[] prisonFiles = Directory.GetFiles(loadLocation, "*.xml", SearchOption.TopDirectoryOnly);

            DataContractSerializer serializer = new DataContractSerializer(typeof(Prison));

            foreach (string prisonLocation in prisonFiles)
            {
                using (FileStream readStream = File.OpenRead(prisonLocation))
                {
                    result.Add((Prison)serializer.ReadObject(readStream));
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Formats a string with the env variables for CreateProcess Win API function.
        /// See env format here: http://msdn.microsoft.com/en-us/library/windows/desktop/ms682653(v=vs.85).aspx
        /// </summary>
        /// <param name="EvnironmantVariables"></param>
        /// <returns></returns>
        private static string BuildEnvironmentVariable(Dictionary<string, string> EvnironmantVariables)
        {
            string ret = null;
            if (EvnironmantVariables.Count > 0)
            {
                foreach (var EnvironmentVariable in EvnironmantVariables)
                {
                    var value = EnvironmentVariable.Value;
                    if (value == null) value = "";

                    if (EnvironmentVariable.Key.Contains('=') || EnvironmentVariable.Key.Contains('\0') || value.Contains('\0'))
                    {
                        throw new ArgumentException("Invalid or restricted charachter", "EvnironmantVariables");
                    }

                    ret += EnvironmentVariable.Key + "=" + value + '\0';
                }


                ret += "\0";
            }

            return ret;
        }

        private static Process[] GetChildPrecesses(int parentId) {
            var result = new List<Process>();

            var query = "Select * From Win32_Process Where ParentProcessId = " + parentId;

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                using (ManagementObjectCollection processList = searcher.Get())
                {
                    foreach (var i in processList)
                    {
                        var pid = Convert.ToInt32(i.GetPropertyValue("ProcessId"));
                        result.Add(Process.GetProcessById(pid));
                    }
                }
            }

            return result.ToArray();
        }

        private static string GetCreateProcessDeletegatePath()
        {
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyDirPath = Directory.GetParent(assemblyPath).FullName;

            return Path.Combine(assemblyDirPath, "Uhuru.Prison.CreateProcessDelegate.exe");
        }

        private static string GetGuardPath()
        {
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyDirPath = Directory.GetParent(assemblyPath).FullName;

            return Path.Combine(assemblyDirPath, "Uhuru.Prison.Guard.exe");
        }
    }
}
