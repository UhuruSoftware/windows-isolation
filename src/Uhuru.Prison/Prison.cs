using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using ProcessPrivileges;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uhuru.Prison.ExecutorService;
using Uhuru.Prison.Restrictions;
using Uhuru.Prison.Utilities;
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
            typeof(Allowances.Httpsys),
            typeof(Restrictions.Memory),
            typeof(Restrictions.Network),
            typeof(Restrictions.WindowStation),
            typeof(Allowances.IISGroup),
        };

        static private string guardSuffix = "-guard";

        List<Rule> prisonCells = null;

        JobObject jobObject = null;

        SafeTokenHandle logonToken = null;

        [DataMember]
        PrisonUser user = null;

        private static volatile bool wasInitialized = false;

        [DataMember]
        internal string desktopName = null;

        public const string changeSessionBaseEndpointAddress = @"net.pipe://localhost/Uhuru.Prison.ExecutorService/Executor";

        private const string databaseLocation = @".\uhuru-prison-db";
        private static string installUtilPath = Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "InstallUtil.exe");

        public JobObject JobObject
        {
            get { return jobObject; }
        }

        public bool IsLocked()
        {
            return this.isLocked;
        }

        [DataMember]
        private bool isLocked = false;

        [DataMember]
        private PrisonRules prisonRules;

        [DataMember]
        public Guid ID
        {
            get;
            set;
        }

        [DataMember]
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
            : this(Guid.NewGuid())
        {
        }

        public Prison(Guid id)
        {
            this.ID = id;
            this.prisonCells = new List<Rule>();

            this.Save();
        }

        private bool CellEnabled(RuleType cellTypeQuery)
        {
            return ((this.prisonRules.CellType & cellTypeQuery) == cellTypeQuery) || ((this.prisonRules.CellType & RuleType.All) == RuleType.All);
        }

        public void Reattach()
        {
            prisonCells = new List<Rule>();

            if (this.user != null && !string.IsNullOrWhiteSpace(this.user.Username))
            {
                Logger.Debug("Prison {0} is attaching to Job Object {1}", this.ID, this.user.Username);

                InitializeJobObject();
            }
            else
            {
                Logger.Debug("Prison {0} has no Job Object to attach to", this.ID);
            }

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

            foreach (Rule cell in this.prisonCells)
            {
                cell.Recover(this);
            }
        }

        public void Lockdown(PrisonRules prisonRules)
        {
            if (this.isLocked)
            {
                throw new InvalidOperationException("This prison is already locked.");
            }

            Logger.Debug("Locking down prison {0}", this.ID);

            Directory.CreateDirectory(prisonRules.PrisonHomePath);

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

            // InitializeSystemVirtualAddressSpaceQuotas();

            this.InitializeJobObject();

            // Lock all cells
            foreach (Rule cell in this.prisonCells)
            {
                if (cell.GetFlag() != RuleType.WindowStation)
                {
                    cell.Apply(this);
                }
            }

            this.CreateUserProfile();

            // Directory.CreateDirectory(prisonRules.PrisonHomePath);
            this.ChangeProfilePath(Path.Combine(prisonRules.PrisonHomePath, "profile"));

            // RunGuard();

            this.isLocked = true;

            this.Save();
        }

        private void CheckGuard()
        {
            using (var guardJob = JobObject.Attach("Global\\" + this.user.Username + Prison.guardSuffix))
            {
            }
        }

        private void InitializeGuard()
        {
            try
            {
                CheckGuard();
            }
            catch (Win32Exception)
            {
                RunGuard();
            }
        }

        private Process RunGuard()
        {
            var psi = new ProcessStartInfo();
            // this is set to true to prevent HANDLE inheritance 
            // http://stackoverflow.com/questions/10638901/create-a-process-and-redirect-its-input-output-and-dont-inherit-socket-handles
            psi.UseShellExecute = true;

            psi.ErrorDialog = false;
            psi.CreateNoWindow = true;

            // TODO: rename TotalPrivateMemoryLimitBytes to a more general term
            psi.FileName = GetGuardPath();
            psi.Arguments = this.user.Username + " " + this.prisonRules.TotalPrivateMemoryLimitBytes;

            var gp = Process.Start(psi);

            // Wait for guard to start
            while (true)
            {
                try
                {
                    CheckGuard();
                    break;
                }
                catch { }

                Thread.Sleep(100);
            }

            return gp;
        }

        private void AddProcessToGuardJobObject(Process p)
        {
            this.InitializeGuard();

            // Careful to close the Guard Job Object, 
            // else it is not guaranteed that the Job Object will not terminate if the Guard exists

            using (var guardJob = JobObject.Attach("Global\\" + this.user.Username + Prison.guardSuffix))
            {
                guardJob.AddProcess(p);
            }

            this.CheckGuard();
        }

        private void TryStopGuard()
        {
            EventWaitHandle dischargeEvent = null;
            EventWaitHandle.TryOpenExisting("Global\\" + "discharge-" + this.user.Username, out dischargeEvent);

            if (dischargeEvent != null)
            {
                dischargeEvent.Set();
            }
        }



        private static void InstallService(string servicePath, Dictionary<string, string> parameters)
        {
            string installCmd = installUtilPath;

            foreach (var p in parameters)
            {
                installCmd += " /" + p.Key + "=" + p.Value;
            }

            installCmd += " " + '"' + servicePath + '"';

            var res = Utilities.Command.ExecuteCommand(installCmd);

            if (res != 0)
            {
                throw new Exception(String.Format("Error installing service {0}, exit code: {1}", installCmd, res));
            }
        }

        private static void UninstallService(string servicePath, Dictionary<string, string> parameters)
        {
            string installCmd = installUtilPath;
            installCmd += " /u ";
            foreach (var p in parameters)
            {
                installCmd += " /" + p.Key + "=" + p.Value;
            }

            installCmd += " " + '"' + servicePath + '"';

            var res = Utilities.Command.ExecuteCommand(installCmd);

            if (res != 0)
            {
                throw new Exception(String.Format("Error installing service {0}, exit code: {1}", installCmd, res));
            }
        }

        private static void InitChangeSessionService(string id)
        {
            InstallService(GetChangeSessionServicePath(), new Dictionary<string, string>() { { "service-id", id } });
            Utilities.Command.ExecuteCommand("net start ChangeSession-" + id);
        }

        private static void RemoveChangeSessionService(string id)
        {
            Utilities.Command.ExecuteCommand("net stop ChangeSession-" + id);
            UninstallService(GetChangeSessionServicePath(), new Dictionary<string, string>() { { "service-id", id } });
        }

        public Process Execute(string filename)
        {
            return this.Execute(filename, string.Empty);
        }

        public Process Execute(string filename, string arguments)
        {
            return this.Execute(filename, arguments, false);
        }

        public Process Execute(string filename, string arguments, bool interactive)
        {
            return this.Execute(filename, arguments, interactive, null);
        }

        public Process Execute(string filename, string arguments, bool interactive, Dictionary<string, string> extraEnvironmentVariables)
        {
            return this.Execute(filename, arguments, interactive, null, null, null, null, null);
        }

        public Process Execute(string filename, string arguments, bool interactive, Dictionary<string, string> extraEnvironmentVariables, string curDir, PipeStream stdinPipeName, PipeStream stdoutPipeName, PipeStream stderrPipeName)
        {
            if (GetCurrentSessionId() == 0)
            {
                var workerProcess = InitializeProcess(filename, arguments, curDir, interactive, extraEnvironmentVariables, stdinPipeName, stdoutPipeName, stderrPipeName);
                ResumeProcess(workerProcess);
                return workerProcess;
            }
            else
            {
                var workerProcess = InitializeProcessWithChagedSession(filename, arguments, curDir, interactive, extraEnvironmentVariables, stdinPipeName, stdoutPipeName, stderrPipeName);
                return workerProcess;
            }
        }

        public Process InitializeProcessWithChagedSession(string filename, string arguments, string curDir, bool interactive, Dictionary<string, string> extraEnvironmentVariables, PipeStream stdinPipeName, PipeStream stdoutPipeName, PipeStream stderrPipeName)
        {
            string tempSeriviceId = Guid.NewGuid().ToString();
            InitChangeSessionService(tempSeriviceId);

            var bind = new NetNamedPipeBinding();
            bind.Security.Mode = NetNamedPipeSecurityMode.Transport;
            bind.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

            var endpoint = new EndpointAddress(changeSessionBaseEndpointAddress + "/" + tempSeriviceId);

            var channelFactory = new ChannelFactory<IExecutor>(bind, endpoint);

            IExecutor remoteSessionExec = channelFactory.CreateChannel();

            var workingProcessId = remoteSessionExec.ExecuteProcess(this, filename, arguments, curDir, extraEnvironmentVariables, stdinPipeName, stdoutPipeName, stderrPipeName);
            var workingProcess = Process.GetProcessById(workingProcessId);
            workingProcess.EnableRaisingEvents = true;

            CloseRemoteSession(remoteSessionExec);

            ResumeProcess(workingProcess);

            RemoveChangeSessionService(tempSeriviceId);

            return workingProcess;
        }

        public Process InitializeProcess(string filename, string arguments, string curDir, bool interactive, Dictionary<string, string> extraEnvironmentVariables, PipeStream stdinPipeName, PipeStream stdoutPipeName, PipeStream stderrPipeName)
        {
            // C with Win32 API example to start a process under a different user: http://msdn.microsoft.com/en-us/library/aa379608%28VS.85%29.aspx

            if (!this.isLocked)
            {
                throw new InvalidOperationException("This prison has to be locked before you can use it.");
            }


            this.InitializeLogonToken();
            this.LoadUserProfileIfNotLoaded();

            var envs = GetDefaultEnvironmentVarialbes();

            // environmentVariables from the method parameters have precedence over the default envs
            if (extraEnvironmentVariables != null)
            {
                foreach (var env in extraEnvironmentVariables)
                {
                    envs[env.Key] = env.Value;
                }
            }

            string envBlock = extraEnvironmentVariables == null ? null : Prison.BuildEnvironmentVariable(envs);

            Logger.Debug("Starting process '{0}' with arguments '{1}' as user '{2}' in working dir '{3}'", filename, arguments, this.user.Username, this.prisonRules.PrisonHomePath);

            if (filename == string.Empty) filename = null;

            if (CellEnabled(RuleType.WindowStation))
            {
                var winStationCell = this.prisonCells.First((a) => { return a.GetFlag() == RuleType.WindowStation; });
                winStationCell.Apply(this);
                //new WindowStation().Apply(this);
            }

            Native.PROCESS_INFORMATION processInfo = NativeCreateProcessAsUser(interactive, filename, arguments, curDir, envBlock, stdinPipeName, stdoutPipeName, stderrPipeName);

            Native.CloseHandle(processInfo.hProcess);
            Native.CloseHandle(processInfo.hThread);

            var workerProcessPid = processInfo.dwProcessId;
            var workerProcess = Process.GetProcessById(workerProcessPid);

            // AccessTokenHandle
            // workerProcess.RemovePrivilege(ProcessPrivileges.Privilege.ChangeNotify);
            // ProcessExtensions.RemovePrivilege(new AccessTokenHandle() , Privilege.ChangeNotify);

            // Tag the process with the Job Object before resuming the process.
            this.jobObject.AddProcess(workerProcess);

            // Add process in the second job object 
            this.AddProcessToGuardJobObject(workerProcess);

            // This would allow the process to query the ExitCode. ref: http://msdn.microsoft.com/en-us/magazine/cc163900.aspx
            workerProcess.EnableRaisingEvents = true;

            return workerProcess;
        }

        private void ResumeProcess(Process workerProcess)
        {
            // Now that the process is tagged with the Job Object so we can resume the thread.
            IntPtr threadHandler = Native.OpenThread(Native.ThreadAccess.SUSPEND_RESUME, false, workerProcess.Threads[0].Id);
            uint resumeResult = Native.ResumeThread(threadHandler);
            Native.CloseHandle(threadHandler);

            if (resumeResult != 1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public void Destroy()
        {
            if (!this.isLocked)
            {
                throw new InvalidOperationException("This prison is not locked.");
            }

            Logger.Debug("Destroying prison {0}", this.ID);

            foreach (var cell in prisonCells)
            {
                cell.Destroy(this);
            }

            this.jobObject.TerminateProcesses(-1);

            this.jobObject.Dispose();
            this.jobObject = null;

            // TODO: Should destroy delete the home directory???
            // Directory.CreateDirectory(prisonRules.PrisonHomePath);

            this.TryStopGuard();
            this.UnloadUserProfileUntilReleased();
            this.DeleteUserProfile();
            this.user.Delete();

            SystemRemoveQuota();

            this.DeletePersistedPrirson();
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
            Logger.Debug("Persisting prison {0}", this.ID);


            string dataLocation = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string dbDirectory = Path.Combine(dataLocation, Prison.databaseLocation);

            Directory.CreateDirectory(dbDirectory);

            string prisonFile = Path.GetFullPath(Path.Combine(dbDirectory, string.Format("{0}.xml", this.ID.ToString("N"))));

            DataContractSerializer serializer = new DataContractSerializer(typeof(Prison));

            using (FileStream writeStream = File.Open(prisonFile, FileMode.Create, FileAccess.Write))
            {
                serializer.WriteObject(writeStream, this);
            }
        }

        private void DeletePersistedPrirson()
        {
            Logger.Debug("Deleting persisted prison {0}", this.ID);

            string dataLocation = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string dbDirectory = Path.Combine(dataLocation, Prison.databaseLocation);

            string prisonFile = Path.GetFullPath(Path.Combine(dbDirectory, string.Format("{0}.xml", this.ID.ToString("N"))));

            File.Delete(prisonFile);
        }

        /// <summary>
        /// This should be a replacement for UnloadUserProfile (http://msdn.microsoft.com/en-us/library/windows/desktop/bb762282%28v=vs.85%29.aspx)
        /// UnloadUserProfile cannot be invoked because the hProfile handle may not be available.
        /// </summary>
        private void UnloadUserProfile()
        {
            InitializeLogonToken();

            var userSid = WindowsUsersAndGroups.GetLocalUserSid(this.User.Username);

            var usersHive = Registry.Users.Handle.DangerousGetHandle();
            var userHive = Registry.Users.OpenSubKey(userSid);
            userHive.Handle.SetHandleAsInvalid();

            var res = Native.UnloadUserProfile(logonToken.DangerousGetHandle(), userHive.Handle.DangerousGetHandle());

            if (res == false)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private void UnloadUserProfileUntilReleased()
        {
            while (this.IsProfileLoaded())
            {
                UnloadUserProfile();
            }
        }

        private void InitializeSystemVirtualAddressSpaceQuotas()
        {
            if (this.Rules.TotalPrivateMemoryLimitBytes > 0)
            {
                SystemVirtualAddressSpaceQuotas.SetPagedPoolQuota
                    (this.Rules.TotalPrivateMemoryLimitBytes, new SecurityIdentifier(this.user.UserSID));

                SystemVirtualAddressSpaceQuotas.SetNonPagedPoolQuota
                    (this.Rules.TotalPrivateMemoryLimitBytes, new SecurityIdentifier(this.user.UserSID));

                SystemVirtualAddressSpaceQuotas.SetPagingFileQuota
                    (this.Rules.TotalPrivateMemoryLimitBytes, new SecurityIdentifier(this.user.UserSID));

                SystemVirtualAddressSpaceQuotas.SetWorkingSetPagesQuota
                    (this.Rules.TotalPrivateMemoryLimitBytes, new SecurityIdentifier(this.user.UserSID));
            }
        }

        private void InitializeJobObject()
        {
            if (this.jobObject != null) return;

            // Create the JobObject
            this.jobObject = new JobObject("Global\\" + this.user.Username);

            if (this.Rules.CPUPercentageLimit > 0)
            {
                this.JobObject.CPUPercentageLimit = this.Rules.CPUPercentageLimit;
            }

            if (this.Rules.TotalPrivateMemoryLimitBytes > 0)
            {
                this.JobObject.JobMemoryLimitBytes = this.Rules.TotalPrivateMemoryLimitBytes;
            }

            if (this.Rules.ActiveProcessesLimit > 0)
            {
                this.JobObject.ActiveProcessesLimit = this.Rules.ActiveProcessesLimit;
            }

            if (this.Rules.PriorityClass.HasValue)
            {
                this.JobObject.PriorityClass = this.Rules.PriorityClass.Value;
            }

            this.jobObject.KillProcessesOnJobClose = true;
        }

        private void InitializeLogonToken()
        {
            if (this.logonToken == null)
            {

                var logonResult = Native.LogonUser(
                    userName: this.user.Username,
                    domain: ".",
                    password: this.user.Password,
                    logonType: Native.LogonType.LOGON32_LOGON_INTERACTIVE, // TODO: consider using Native.LogonType.LOGON32_LOGON_SERVICE see: http://blogs.msdn.com/b/winsdk/archive/2013/03/22/how-to-launch-a-process-as-a-full-administrator-when-uac-is-enabled.aspx
                    logonProvider: Native.LogonProvider.LOGON32_PROVIDER_DEFAULT,
                    token: out logonToken
                    );

                if (!logonResult)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        // Loads the user's profile
        // Note: Beware of Paged Pool memory leaks!!! Unload the user profile when destroying the prison.
        // For each load called there must be a corresponding unload called (independent to process space).
        private void LoadUserProfile()
        {
            this.InitializeLogonToken();

            Native.PROFILEINFO profileInfo = new Native.PROFILEINFO();
            profileInfo.dwSize = Marshal.SizeOf(profileInfo);
            profileInfo.lpUserName = this.user.Username;

            // PI_NOUI 0x00000001 // Prevents displaying of messages
            profileInfo.dwFlags = 0x1;

            profileInfo.lpProfilePath = null;
            profileInfo.lpDefaultPath = null;
            profileInfo.lpPolicyPath = null;
            profileInfo.lpServerName = null;

            Boolean loadSuccess = Native.LoadUserProfile(logonToken.DangerousGetHandle(), ref profileInfo);


            if (!loadSuccess)
            {
                Logger.Error("LoadUserProfile failed with error code: {0} for prison {1}", Marshal.GetLastWin32Error(), this.ID);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (profileInfo.hProfile == IntPtr.Zero)
            {
                Logger.Error("LoadUserProfile failed HKCU handle was not loaded. Error code: {0} for prison {1}", Marshal.GetLastWin32Error(), this.ID);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private void LoadUserProfileIfNotLoaded()
        {
            if (!this.IsProfileLoaded())
            {
                LoadUserProfile();
            }
        }

        private static int GetCurrentSessionId()
        {
            return 0; // Set for windows-warden
            return Process.GetCurrentProcess().SessionId;
        }

        // Check if the profile is loaded.
        // This is useful to load the profile only once.
        private bool IsProfileLoaded()
        {
            var userSid = WindowsUsersAndGroups.GetLocalUserSid(this.User.Username);

            // If a profile is loaded the Registry hive will be loaded in HKEY_USERS\{User-SID}
            var res = Registry.Users.GetSubKeyNames().Contains(userSid);

            return res;
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

            string dataLocation = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string loadLocation = Path.GetFullPath(Path.Combine(dataLocation, Prison.databaseLocation));

            Logger.Debug("Loading prison database from {0}", loadLocation);

            Directory.CreateDirectory(loadLocation);

            string[] prisonFiles = Directory.GetFiles(loadLocation, "*.xml", SearchOption.TopDirectoryOnly);

            Logger.Debug("Found {0} prison entries", prisonFiles.Length);

            DataContractSerializer serializer = new DataContractSerializer(typeof(Prison));

            foreach (string prisonLocation in prisonFiles)
            {
                using (FileStream readStream = File.OpenRead(prisonLocation))
                {
                    Prison loadedPrison = (Prison)serializer.ReadObject(readStream);
                    result.Add(loadedPrison);
                }
            }

            return result.ToArray();
        }

        public static Prison LoadPrisonAndAttach(Guid prisonId)
        {
            Prison loadedPrison = Prison.Load().First(p => p.ID == prisonId);

            if (loadedPrison != null)
            {
                loadedPrison.Reattach();
                return loadedPrison;
            }
            else
            {
                return null;
            }
        }

        public static Prison LoadPrisonNoAttach(Guid prisonId)
        {
            Prison loadedPrison = Prison.Load().First(p => p.ID == prisonId);

            if (loadedPrison != null)
            {
                return loadedPrison;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Formats a string with the env variables for CreateProcess Win API function.
        /// See env format here: http://msdn.microsoft.com/en-us/library/windows/desktop/ms682653(v=vs.85).aspx
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        private static string BuildEnvironmentVariable(Dictionary<string, string> environmentVariables)
        {
            string ret = null;
            if (environmentVariables.Count > 0)
            {
                foreach (var EnvironmentVariable in environmentVariables)
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

        private static Process[] GetChildPrecesses(int parentId)
        {
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

        private static string GetChangeSessionServicePath()
        {
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyDirPath = Directory.GetParent(assemblyPath).FullName;

            return Path.Combine(assemblyDirPath, "Uhuru.Prison.ChangeSession.exe");
        }

        public Dictionary<string, string> GetDefaultEnvironmentVarialbes()
        {
            Dictionary<string, string> res = new Dictionary<string, string>();

            var envblock = IntPtr.Zero;

            if (!Native.CreateEnvironmentBlock(out envblock, logonToken.DangerousGetHandle(), false))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // source: http://www.pinvoke.net/default.aspx/userenv.createenvironmentblock
            try
            {
                StringBuilder testData = new StringBuilder("");

                unsafe
                {
                    short* start = (short*)envblock.ToPointer();
                    bool done = false;
                    short* current = start;
                    while (!done)
                    {
                        if ((testData.Length > 0) && (*current == 0) && (current != start))
                        {
                            String data = testData.ToString();
                            int index = data.IndexOf('=');
                            if (index == -1)
                            {
                                res.Add(data, "");
                            }
                            else if (index == (data.Length - 1))
                            {
                                res.Add(data.Substring(0, index), "");
                            }
                            else
                            {
                                res.Add(data.Substring(0, index), data.Substring(index + 1));
                            }
                            testData.Length = 0;
                        }
                        if ((*current == 0) && (current != start) && (*(current - 1) == 0))
                        {
                            done = true;
                        }
                        if (*current != 0)
                        {
                            testData.Append((char)*current);
                        }
                        current++;
                    }
                }
            }
            finally
            {
                Native.DestroyEnvironmentBlock(envblock);
            }

            return res;
        }

        private void CreateUserProfile()
        {
            //StringBuilder pathBuf = new StringBuilder(1024);
            //uint pathLen = (uint)pathBuf.Capacity;

            //int result = Native.CreateProfile(this.user.UserSID, this.user.Username, pathBuf, pathLen);
            //if (result != 0) // S_OK
            //{
            //    throw new Win32Exception(Marshal.GetLastWin32Error());
            //}

            this.LoadUserProfile();
            this.UnloadUserProfile();
        }

        private void ChangeProfilePath(string destination)
        {
            // Strategies for changes profile path:
            // http://programmaticallyspeaking.com/changing-the-user-profile-path-in-windows-7.html
            // 1. Create the user's profile in the default directory. Then change the value of ProfileImagePath from the following reg key:
            // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{User-SID}
            // 2. Change the default profile directory before creating the reg key and the revert it back. Mange the ProfiliesDirectory value from
            // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList
            // 3. Use symbolic links from the default profile path to the desired destination. 
            // 4. Use roaming profile: http://stackoverflow.com/questions/2015103/creating-roaming-user-profiles-programmatically-on-windows-2008

            this.UnloadUserProfileUntilReleased();

            StringBuilder pathBuf = new StringBuilder(1024);
            GetNativeUserProfileDirectory(pathBuf);

            string currentProfileDir = pathBuf.ToString();

            ChangeRegistryUserProfile(destination);

            Directory.Move(currentProfileDir, destination);
        }

        private void DeleteUserProfile()
        {
            string userSid = this.user.UserSID;

            int retries = 30;
            int errorCode = 0;

            while (retries > 0)
            {
                if (!Native.DeleteProfile(userSid, null, null))
                {
                    errorCode = Marshal.GetLastWin32Error();

                    // Error Code 2: The user profile was not created or was already deleted
                    if (errorCode == 2)
                    {
                        return;
                    }
                    // Error Code 87: The user profile is still loaded.
                    else
                    {
                        retries--;
                        Thread.Sleep(200);
                    }
                }
                else
                {
                    return;
                }
            }
            throw new Win32Exception(errorCode);
        }

        private void GetNativeUserProfileDirectory(StringBuilder pathBuf)
        {
            uint pathLen = (uint)pathBuf.Capacity;
            Native.GetUserProfileDirectory(this.logonToken.DangerousGetHandle(), pathBuf, ref pathLen);
        }

        private void ChangeRegistryUserProfile(string destination)
        {
            var userProfKey =
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).
                OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\" + user.UserSID, true);

            userProfKey.SetValue("ProfileImagePath", destination, RegistryValueKind.ExpandString);

        }

        private Native.PROCESS_INFORMATION NativeCreateProcessAsUser(bool interactive, string filename, string arguments, string curDir, string envBlock, PipeStream stdinPipeName, PipeStream stdoutPipeName, PipeStream stderrPipeName)
        {
            var startupInfo = new Native.STARTUPINFO();
            var processInfo = new Native.PROCESS_INFORMATION();

            PipeStream stdinPipe = null;
            PipeStream stdoutPipe = null;
            PipeStream stderrPipe = null;

            startupInfo = new Native.STARTUPINFO();

            if (CellEnabled(RuleType.WindowStation))
            {
                startupInfo.lpDesktop = this.desktopName;
            }

            Native.ProcessCreationFlags creationFlags = Native.ProcessCreationFlags.ZERO_FLAG;

            // Exclude flags
            creationFlags &=
                ~Native.ProcessCreationFlags.CREATE_PRESERVE_CODE_AUTHZ_LEVEL &
                ~Native.ProcessCreationFlags.CREATE_BREAKAWAY_FROM_JOB;

            // Include flags
            creationFlags |=
                Native.ProcessCreationFlags.CREATE_DEFAULT_ERROR_MODE |
                Native.ProcessCreationFlags.CREATE_NEW_PROCESS_GROUP |
                Native.ProcessCreationFlags.CREATE_SUSPENDED |
                Native.ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT |
                Native.ProcessCreationFlags.CREATE_NEW_CONSOLE;

            // TODO: extra steps for interactive to work:
            // http://blogs.msdn.com/b/winsdk/archive/2013/05/01/how-to-launch-a-process-interactively-from-a-windows-service.aspx
            if (interactive)
            {
                startupInfo.lpDesktop = "";
            }
            else
            {
                // creationFlags |= Native.ProcessCreationFlags.CREATE_NO_WINDOW;

                // startupInfo.dwFlags |= 0x00000100; // STARTF_USESTDHANDLES

                // Dangerous and maybe insecure to give a handle like that an untrusted processes
                //startupInfo.hStdInput = Native.GetStdHandle(Native.STD_INPUT_HANDLE);
                //startupInfo.hStdOutput = Native.GetStdHandle(Native.STD_OUTPUT_HANDLE);
                //startupInfo.hStdError = Native.GetStdHandle(Native.STD_ERROR_HANDLE);

                if (stdinPipeName != null || stdoutPipeName != null || stderrPipeName != null)
                {
                    startupInfo.dwFlags |= 0x00000100; // STARTF_USESTDHANDLES
                }

                if (stdinPipeName != null)
                {
                    startupInfo.hStdInput = GetHandleFromPipe(stdinPipeName);
                }

                if (stdoutPipeName != null)
                {
                    startupInfo.hStdOutput = GetHandleFromPipe(stdoutPipeName);
                }

                if (stderrPipeName != null)
                {
                    startupInfo.hStdError = GetHandleFromPipe(stderrPipeName);
                }
            }

            Native.SECURITY_ATTRIBUTES processAttributes = new Native.SECURITY_ATTRIBUTES();
            Native.SECURITY_ATTRIBUTES threadAttributes = new Native.SECURITY_ATTRIBUTES();
            processAttributes.nLength = Marshal.SizeOf(processAttributes);
            threadAttributes.nLength = Marshal.SizeOf(threadAttributes);

            var createProcessSuc = NativeCreateProcessAsUser(
                hToken: logonToken,
                lpApplicationName: filename,
                lpCommandLine: arguments,
                lpProcessAttributes: ref processAttributes,
                lpThreadAttributes: ref threadAttributes,
                bInheritHandles: true,
                dwCreationFlags: creationFlags,
                lpEnvironment: envBlock,
                lpCurrentDirectory: curDir,
                lpStartupInfo: ref startupInfo,
                lpProcessInformation: out processInfo);

            if (createProcessSuc == false)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // TODO: use finally
            if (stdinPipe != null) stdinPipe.Dispose(); stdinPipe = null;
            if (stdoutPipe != null) stdoutPipe.Dispose(); stdoutPipe = null;
            if (stderrPipe != null) stderrPipe.Dispose(); stderrPipe = null;

            return processInfo;
        }

        private static IntPtr GetHandleFromPipe(PipeStream ps)
        {
            return ps.SafePipeHandle.DangerousGetHandle();
        }


        private static bool NativeCreateProcessAsUser(
            SafeTokenHandle hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref Uhuru.Prison.Native.SECURITY_ATTRIBUTES lpProcessAttributes,
            ref Uhuru.Prison.Native.SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            Uhuru.Prison.Native.ProcessCreationFlags dwCreationFlags,
            string lpEnvironment,
            string lpCurrentDirectory,
            ref Uhuru.Prison.Native.STARTUPINFO lpStartupInfo,
            out Uhuru.Prison.Native.PROCESS_INFORMATION lpProcessInformation
            )
        {
            return Native.CreateProcessAsUser(
                    hToken.DangerousGetHandle(),
                    lpApplicationName,
                    lpCommandLine,
                    ref lpProcessAttributes,
                    ref lpThreadAttributes,
                    bInheritHandles,
                    dwCreationFlags,
                    lpEnvironment,
                    lpCurrentDirectory,
                    ref lpStartupInfo,
                    out lpProcessInformation
                );
        }
        private void SystemRemoveQuota()
        {
            SystemVirtualAddressSpaceQuotas.RemoveQuotas(new SecurityIdentifier(this.user.UserSID));
        }

        private void CloseRemoteSession(IExecutor remoteSessionExec)
        {
            ((ICommunicationObject)remoteSessionExec).Close();
        }
    }
}
