// -----------------------------------------------------------------------
// <copyright file="JobObject.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.Prison.Utilities.WindowsJobObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;

    /// <summary>
    /// Is a class that manages a Windows Job Object. Job Objects allows to group a number of processes and
    /// perform some aggregate operations. It is a good tool for enforcing resource sandboxing for processes.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Preserve WinAPI naming.")]
    [SecurityPermission(SecurityAction.LinkDemand), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2126:TypeLinkDemandsRequireInheritanceDemands", Justification = "Appropriate to suppress."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2135:SecurityRuleSetLevel2MethodsShouldNotBeProtectedWithLinkDemandsFxCopRule", Justification = "Appropriate to suppress.")]
    //// [SecurityCriticalAttribute]
    public class JobObject : IDisposable
    {
        /// <summary>
        /// Name of the Job Object
        /// </summary>
        private string name;

        /// <summary>
        /// The Windows Handle
        /// </summary>
        private JobObjectHandle jobHandle;

        /// <summary>
        /// Flag to kill processes on job close.
        /// </summary>
        private bool killProcessesOnJobClose = true;

        /// <summary>
        /// Die on unhandled exception.
        /// </summary>
        private bool dieOnUnhandledException = false;

        /// <summary>
        /// Allow child process breakaway.
        /// </summary>
        private bool allowChildProcessesBreakaway = false;

        /// <summary>
        /// Breakaway child processes by default.
        /// </summary>
        private bool alwaysBreakawayChildProcesses = false;

        /// <summary>
        /// Maximum number of active processes.
        /// </summary>
        private uint activeProcessesLimit = 0;

        /// <summary>
        /// The memory limit of the Job Object.
        /// </summary>
        private long jobMemoryLimit = 0;

        /// <summary>
        /// The memory limit of each process in the Job Object.
        /// </summary>
        private long processMemoryLimit = 0;

        /// <summary>
        /// The CPU percentage limit for the entire Job Object.
        /// </summary>
        private float cpuPercentageLimit = 0;

        /// <summary>
        /// Flag if the job processor user time limit has changed.
        /// </summary>
        private bool jobUserTimeLimitChanged = false;

        /// <summary>
        /// The processor user time limit for the job.
        /// </summary>
        private long jobUserTimeLimit = 0;

        /// <summary>
        /// The user time limit per each process in the job.
        /// </summary>
        private long processUserTimeLimit = 0;

        /// <summary>
        /// The priority class of the Job Object.
        /// </summary>
        private uint priorityClass = 0;

        /// <summary>
        /// The scheduling class.
        /// </summary>
        private uint schedulingClass = 0;

        /// <summary>
        /// The processor affinity.
        /// </summary>
        private IntPtr affinity = IntPtr.Zero;

        /// <summary>
        /// The read IO operations count for the whole job.
        /// </summary>
        private long ioReadOperationsCount = 0;

        /// <summary>
        /// The write IO operations count for the whole job.
        /// </summary>
        private long ioWriteOperationsCount = 0;

        /// <summary>
        /// Other IO operations count for the while job.
        /// </summary>
        private long ioOtherOperationsCount = 0;

        /// <summary>
        /// Total IO bytes read by the job.
        /// </summary>
        private long ioReadBytes = 0;

        /// <summary>
        /// Total IO bytes written by the job.
        /// </summary>
        private long ioWriteBytes = 0;

        /// <summary>
        /// Total IO bytes used in other operations.
        /// </summary>
        private long ioOtherBytes = 0;

        /// <summary>
        /// Peak memory usage by the job.
        /// </summary>
        private ulong peakJobMemory = 0;

        /// <summary>
        /// Peak memory usage by a process.
        /// </summary>
        private ulong peakProcessMemory = 0;

        /// <summary>
        /// Total user processor time used by the job.
        /// </summary>
        private ulong userProcessorTime = 0;

        /// <summary>
        /// Total kernel processor time used by the job.
        /// </summary>
        private ulong kernelProcessorTime = 0;

        /// <summary>
        /// Total processes that was in the job object.
        /// </summary>
        private uint totalProcesses = 0;

        /// <summary>
        /// Active processes in the job object.
        /// </summary>
        private uint activeProcesses = 0;

        /// <summary>
        /// Processes killed due to job object restrictions.
        /// </summary>
        private uint totalTerminatedProcesses = 0;

        /// <summary>
        /// The processes in the job.
        /// </summary>
        private Process[] jobProcesses;

        /// <summary>
        /// Attach to a named Job Object.
        /// </summary>
        /// <returns></returns>
        public static JobObject Attach(string jobObjectName)
        {
            if (string.IsNullOrEmpty(jobObjectName))
            {
                throw new ArgumentNullException("jobObjectName");
            }

            // JOB_OBJECT_ALL_ACCESS = 0x1F001F
            var jobHandle = NativeMethods.OpenJobObject(0x1F001F, false, jobObjectName);
            if (jobHandle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, "OpenJobObject failed.");
            }

            return new JobObject(jobHandle, jobObjectName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobObject"/> class. No Windows Job Object is created.
        /// </summary>
        private JobObject(JobObjectHandle jobObject, string jobObjectName)
        {
            this.jobHandle = jobObject;
            this.name = jobObjectName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobObject"/> class. The Windows Job Object is unnamed.
        /// </summary>
        public JobObject()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobObject"/> class. If a Job Object with the specified name exists,
        /// then the Job Object is opened; if not, the Job Object with the specified named is opened.
        /// </summary>
        /// <param name="jobObjectName">Name of the job object.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "CreateJobObject", Justification = "Appropriate to suppress."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "OpenJobObject", Justification = "Appropriate to suppress.")]
        public JobObject(string jobObjectName)
        {
            if (string.IsNullOrEmpty(jobObjectName))
            {
                jobObjectName = null;
            }

            this.name = jobObjectName;

            this.jobHandle = NativeMethods.CreateJobObject(IntPtr.Zero, jobObjectName);
            if (this.jobHandle.IsInvalid)
            {
                if (jobObjectName == null)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "CreateJobObject failed.");
                }
                else
                {
                    // JOB_OBJECT_ALL_ACCESS = 0x1F001F
                    this.jobHandle = NativeMethods.OpenJobObject(0x1F001F, false, jobObjectName);
                    if (this.jobHandle.IsInvalid)
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "OpenJobObject failed.");
                    }
                }
            }

            this.UpdateExtendedLimit();
        }

        /// <summary>
        /// Finalizes an instance of the JobObject class. Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="JobObject"/> is reclaimed by garbage collection.
        /// </summary>
        ~JobObject()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to kill the processes when the Job Object is closed.
        /// </summary>
        /// <value>
        ///  <c>true</c> if [kill processes on job close]; otherwise, <c>false</c>.
        /// </value>
        public bool KillProcessesOnJobClose
        {
            get
            {
                return this.killProcessesOnJobClose;
            }

            set
            {
                this.killProcessesOnJobClose = value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a process to die on an unhandled exception.
        /// </summary>
        /// <value>
        ///  <c>true</c> if [die on unhandled exception]; otherwise, <c>false</c>.
        /// </value>
        public bool DieOnUnhandledException
        {
            get
            {
                return this.dieOnUnhandledException;
            }

            set
            {
                this.dieOnUnhandledException = value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether processes are allowed to create processes outside the Job Object.
        /// </summary>
        /// <value>
        ///  <c>true</c> if [allow child processes breakaway]; otherwise, <c>false</c>.
        /// </value>
        public bool AllowChildProcessesBreakaway
        {
            get
            {
                return this.allowChildProcessesBreakaway;
            }

            set
            {
                this.allowChildProcessesBreakaway = value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether child processes are not added to the Job Object.
        /// </summary>
        /// <value>
        ///  <c>true</c> if [always breakaway child processes]; otherwise, <c>false</c>.
        /// </value>
        public bool AlwaysBreakawayChildProcesses
        {
            get
            {
                return this.alwaysBreakawayChildProcesses;
            }

            set
            {
                this.alwaysBreakawayChildProcesses = value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets the active processes in the Job Object. Set to 0 (zero) to disable the limit.
        /// </summary>
        /// <value>
        /// The active processes.
        /// </value>
        public int ActiveProcessesLimit
        {
            get
            {
                return (int)this.activeProcessesLimit;
            }

            set
            {
                this.activeProcessesLimit = (uint)value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets the memory in bytes limit enforced per process. Set to 0 (zero) to disable the limit.
        /// </summary>
        /// <value>
        /// The process memory limit.
        /// </value>
        public long ProcessMemoryLimit
        {
            get
            {
                return (long)this.processMemoryLimit;
            }

            set
            {
                this.processMemoryLimit = (long)value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets the percentage of CPU cycles allowed to be used by all processes in the JobObject. 
        /// Should be a value between 0 and 100.
        /// Set to 0 (zero) to disable the limit.
        /// </summary>
        /// <value>
        /// The process memory limit.
        /// </value>
        public float CPUPercentageLimit
        {
            get
            {
                return this.cpuPercentageLimit;
            }

            set
            {
                if ((value < 0) || (value > 100))
                {
                    throw new InvalidOperationException("Invalid CPU percentage limit for Job Object - the value has to be in the interval (0, 100)");
                }

                this.cpuPercentageLimit = value;
                this.UpdateCPULimits();
            }
        }

        /// <summary>
        /// Gets or sets the memory limit in bytes of the entire Job Object. Set to 0 (zero) to disable the limit.
        /// </summary>
        /// <value>
        /// The job memory limit.
        /// </value>
        public long JobMemoryLimitBytes
        {
            get
            {
                return this.jobMemoryLimit;
            }

            set
            {
                this.jobMemoryLimit = value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets the process user time limit. It is enforced per process. Set to 0 (zero) to disable the limit.
        /// </summary>
        /// <value>
        /// The process user time limit.
        /// </value>
        public TimeSpan ProcessUserTimeLimit
        {
            get
            {
                return new TimeSpan(this.processUserTimeLimit);
            }

            set
            {
                this.processUserTimeLimit = value.Ticks;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets the Job Object user time limit. Every process user time is accounted. Set to 0 (zero) to disable the limit.
        /// </summary>
        /// <value>
        /// The job user time limit.
        /// </value>
        public TimeSpan JobUserTimeLimit
        {
            get
            {
                return new TimeSpan(this.jobUserTimeLimit);
            }

            set
            {
                this.jobUserTimeLimit = value.Ticks;
                this.jobUserTimeLimitChanged = true;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets the priority class of the Job Object.
        /// </summary>
        /// <value>
        /// The priority class.
        /// </value>
        public ProcessPriorityClass PriorityClass
        {
            get
            {
                return (ProcessPriorityClass)this.priorityClass;
            }

            set
            {
                this.priorityClass = (uint)value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets the scheduling class of the JobObject.
        /// </summary>
        /// <value>
        /// The scheduling class.
        /// </value>
        public int SchedulingClass
        {
            get
            {
                return (int)this.schedulingClass;
            }

            set
            {
                this.schedulingClass = (uint)value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets or sets the processor affinity, enforced for every process.
        /// </summary>
        /// <value>
        /// The affinity.
        /// </value>
        public IntPtr Affinity
        {
            get
            {
                return this.affinity;
            }

            set
            {
                this.affinity = value;
                this.UpdateExtendedLimit();
            }
        }

        /// <summary>
        /// Gets the total IO bytes read by every process in the Job Object.
        /// </summary>
        public long IOReadBytes
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return this.ioReadBytes;
            }
        }

        /// <summary>
        /// Gets the total IO bytes written by every process in the Job Object.
        /// </summary>
        public long IOWriteBytes
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return this.ioWriteBytes;
            }
        }

        /// <summary>
        /// Gets the total IO bytes used in other operations by every process in the Job Object.
        /// </summary>
        public long IOOtherBytes
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return this.ioOtherBytes;
            }
        }

        /// <summary>
        /// Gets the IO read operations count preformed by every process in the Job Object.
        /// </summary>
        public long IOReadOperationsCount
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return this.ioReadOperationsCount;
            }
        }

        /// <summary>
        /// Gets the IO write operations count preformed by every process in the Job Object.
        /// </summary>
        public long IOWriteOperationsCount
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return this.ioWriteOperationsCount;
            }
        }

        /// <summary>
        /// Gets the other IO operations count preformed by every process in the Job Object.
        /// </summary>
        public long IOOtherOperationsCount
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return this.ioOtherOperationsCount;
            }
        }

        /// <summary>
        /// Gets the peak memory in bytes used by the Job Object at any given time.
        /// </summary>
        public long PeakJobMemory
        {
            get
            {
                this.QueryExtendedLimitInformation();
                return (long)this.peakJobMemory;
            }
        }

        /// <summary>
        /// Gets the peak memory in bytes used by a process.
        /// </summary>
        public long PeakProcessMemory
        {
            get
            {
                this.QueryExtendedLimitInformation();
                return (long)this.peakProcessMemory;
            }
        }

        /// <summary>
        /// Gets the total processor time used by each process.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return new TimeSpan((long)this.userProcessorTime + (long)this.kernelProcessorTime);
            }
        }

        /// <summary>
        /// Gets the user processor time used by each process.
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return new TimeSpan((long)this.userProcessorTime);
            }
        }

        /// <summary>
        /// Gets the kernel processor time used by each process.
        /// </summary>
        public TimeSpan KernelProcessorTime
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return new TimeSpan((long)this.kernelProcessorTime);
            }
        }

        /// <summary>
        /// Gets the total processes that was in the job object.
        /// </summary>
        public int TotalProcesses
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return (int)this.totalProcesses;
            }
        }

        /// <summary>
        /// Gets active processes in the job object.
        /// </summary>
        public int ActiveProcesses
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return (int)this.activeProcesses;
            }
        }

        /// <summary>
        /// Gets processes killed due to job object restrictions.
        /// </summary>
        public int TotalTerminatedProcesses
        {
            get
            {
                this.QueryBasicAndIoAccounting();
                return (int)this.totalTerminatedProcesses;
            }
        }

        /// <summary>
        /// Gets the working set memory in bytes of the Job Object.
        /// </summary>
        public long WorkingSetMemory
        {
            get
            {
                this.QueryProcessIds();
                long res = 0;
                foreach (Process p in this.jobProcesses)
                {
                    try
                    {
                        res += p.WorkingSet64;
                    }
                    catch (InvalidOperationException) { }
                }

                return res;
            }
        }

        /// <summary>
        /// Gets the virtual memory in bytes.
        /// </summary>
        public long VirtualMemory
        {
            get
            {
                this.QueryProcessIds();
                long res = 0;
                foreach (Process p in this.jobProcesses)
                {
                    try
                    {
                        res += p.VirtualMemorySize64;
                    }
                    catch (InvalidOperationException) { }
                }

                return res;
            }
        }

        /// <summary>
        /// Gets the private memory in bytes.
        /// Coresponds to Private Bytes in Process Hacker, Commit Size in Task Manager.
        /// </summary>
        public long PrivateMemory
        {
            get
            {
                this.QueryProcessIds();
                long res = 0;
                foreach (Process p in this.jobProcesses)
                {
                    try
                    {
                        res += p.PrivateMemorySize64;
                    }
                    catch (InvalidOperationException) { }
                }

                return res;
            }
        }

        /// <summary>
        /// Gets the paged memory in bytes.
        /// </summary>
        public long PagedMemory
        {
            get
            {
                this.QueryProcessIds();
                long res = 0;
                foreach (Process p in this.jobProcesses)
                {
                    try
                    {
                        res += p.PagedMemorySize64;
                    }
                    catch (InvalidOperationException) { }
                }

                return res;
            }
        }

        /// <summary>
        /// Gets the paged system memory in bytes.
        /// </summary>
        public long PagedSystemMemory
        {
            get
            {
                this.QueryProcessIds();
                long res = 0;
                foreach (Process p in this.jobProcesses)
                {
                    try
                    {
                        res += p.PagedSystemMemorySize64;
                    }
                    catch (InvalidOperationException) { }
                }

                return res;
            }
        }

        /// <summary>
        /// Gets the non paged system memory in bytes.
        /// </summary>
        public long NonPagedSystemMemory
        {
            get
            {
                this.QueryProcessIds();
                long res = 0;
                foreach (Process p in this.jobProcesses)
                {
                    try
                    {
                        res += p.NonpagedSystemMemorySize64;
                    }
                    catch (InvalidOperationException) { }
                }

                return res;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Adds a process to the current Job Object, for which the Job Object limits apply.
        /// </summary>
        /// <param name="process">The process to be added.</param>
        public void AddProcess(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException("process");
            }

            this.AddProcess(process.Handle);
        }

        /// <summary>
        /// Determines whether the specified process is in the Job Object
        /// </summary>
        /// <param name="process">The process to be checked for.</param>
        /// <returns>
        ///   <c>true</c> if the specified process has the process; otherwise, <c>false</c>.
        /// </returns>
        public bool HasProcess(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException("process");
            }

            return this.HasProcess(process.Handle);
        }

        /// <summary>
        /// Terminates all the processes in the Job Object.
        /// </summary>
        /// <param name="exitCode">The exit code.</param>
        public void TerminateProcesses(int exitCode)
        {
            NativeMethods.TerminateJobObject(this.jobHandle, (uint)exitCode);
        }

        /// <summary>
        /// Gets all the processes included in the Job Object.
        /// </summary>
        /// <returns>The list of processes.</returns>
        public Process[] GetJobProcesses()
        {
            this.QueryProcessIds();
            return this.jobProcesses;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.jobHandle.Close();
            }
        }

        /// <summary>
        /// Determines whether the specified process handle is included in the Job Object.
        /// </summary>
        /// <param name="processHandle">The process handle.</param>
        /// <returns>
        ///   <c>true</c> if the specified process handle has process; otherwise, <c>false</c>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "IsProcessInJob", Justification = "Appropriate to suppress.")]
        private bool HasProcess(IntPtr processHandle)
        {
            bool result;
            bool success = NativeMethods.IsProcessInJob(processHandle, this.jobHandle, out result);
            if (success == false)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, "IsProcessInJob failed.");
            }

            return result;
        }

        /// <summary>
        /// Adds the process handle to the Job object.
        /// </summary>
        /// <param name="processHandle">The process handle.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "AssignProcessToJobObject", Justification = "Appropriate to suppress.")]
        private void AddProcess(IntPtr processHandle)
        {
            bool success = NativeMethods.AssignProcessToJobObject(this.jobHandle, processHandle);
            if (success == false)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, "AssignProcessToJobObject failed.");
            }
        }

        /// <summary>
        /// Queries the process ids.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "QueryInformationJobObject", Justification = "Appropriate to suppress.")]
        private void QueryProcessIds()
        {
            NativeMethods.JOBOBJECT_BASIC_PROCESS_ID_LIST processList = new NativeMethods.JOBOBJECT_BASIC_PROCESS_ID_LIST();
            processList.NumberOfAssignedProcesses = NativeMethods.JOBOBJECT_BASIC_PROCESS_ID_LIST.MaxProcessListLength;
            processList.NumberOfProcessIdsInList = 0;
            processList.ProcessIdList = null;

            int processListLength = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_BASIC_PROCESS_ID_LIST));
            IntPtr processListPtr = Marshal.AllocHGlobal(processListLength);

            try
            {
                Marshal.StructureToPtr(processList, processListPtr, false);

                bool success = NativeMethods.QueryInformationJobObject(this.jobHandle, NativeMethods.JobObjectInfoClass.JobObjectBasicProcessIdList, processListPtr, (uint)processListLength, IntPtr.Zero);

                if (success == false)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "QueryInformationJobObject failed.");
                }

                processList = (NativeMethods.JOBOBJECT_BASIC_PROCESS_ID_LIST)Marshal.PtrToStructure(processListPtr, typeof(NativeMethods.JOBOBJECT_BASIC_PROCESS_ID_LIST));

                List<Process> pss = new List<Process>();

                for (int i = 0; i < processList.NumberOfProcessIdsInList; i++)
                {
                    try
                    {
                        pss.Add(Process.GetProcessById((int)processList.ProcessIdList[i]));
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                this.jobProcesses = pss.ToArray();
            }
            finally
            {
                Marshal.FreeHGlobal(processListPtr);
            }
        }

        /// <summary>
        /// Updates CPU Limit restrictions.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "SetInformationJobObject", Justification = "Appropriate to suppress.")]
        private void UpdateCPULimits()
        {
            NativeMethods.JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuRateControlLimit = new NativeMethods.JOBOBJECT_CPU_RATE_CONTROL_INFORMATION();

            if (this.cpuPercentageLimit != 0)
            {
                cpuRateControlLimit.ControlFlags = (uint)(
                    NativeMethods.JOBOBJECT_CPU_RATE_CONTROL_INFORMATION.CpuRateControlFlags.JOB_OBJECT_CPU_RATE_CONTROL_ENABLE |
                    NativeMethods.JOBOBJECT_CPU_RATE_CONTROL_INFORMATION.CpuRateControlFlags.JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP
                    );

                cpuRateControlLimit.CpuRate = (uint)Math.Floor(this.cpuPercentageLimit * 100);

                int cpuRateControlLimitLength = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_CPU_RATE_CONTROL_INFORMATION));
                IntPtr cpuRateControlLimitPtr = Marshal.AllocHGlobal(cpuRateControlLimitLength);

                try
                {
                    Marshal.StructureToPtr(cpuRateControlLimit, cpuRateControlLimitPtr, false);

                    bool success = NativeMethods.SetInformationJobObject(this.jobHandle, NativeMethods.JobObjectInfoClass.JobObjectCpuRateControlInformation, cpuRateControlLimitPtr, (uint)cpuRateControlLimitLength);

                    if (success == false)
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "SetInformationJobObject failed for setting CPU limits.");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(cpuRateControlLimitPtr);
                }
            }
        }


        /// <summary>
        /// Updates the extended limit.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "SetInformationJobObject", Justification = "Appropriate to suppress.")]
        private void UpdateExtendedLimit()
        {
            NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION extendedLimit = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION basicLimit = new NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION();

            basicLimit.LimitFlags = 0;

            if (this.killProcessesOnJobClose)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
            }

            if (this.dieOnUnhandledException)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION;
            }

            if (this.allowChildProcessesBreakaway)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_BREAKAWAY_OK;
            }

            if (this.alwaysBreakawayChildProcesses)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK;
            }

            if (this.activeProcessesLimit != 0)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_ACTIVE_PROCESS;
                basicLimit.ActiveProcessLimit = this.activeProcessesLimit;
            }

            if (this.processMemoryLimit != 0)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_PROCESS_MEMORY;
                extendedLimit.ProcessMemoryLimit = (IntPtr)this.processMemoryLimit;
            }

            if (this.jobMemoryLimit != 0)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_JOB_MEMORY;
                extendedLimit.JobMemoryLimit = (IntPtr)this.jobMemoryLimit;
            }

            if (this.processUserTimeLimit != 0)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_PROCESS_TIME;
                basicLimit.PerProcessUserTimeLimit = this.processUserTimeLimit;
            }

            if (this.jobUserTimeLimit != 0)
            {
                if (this.jobUserTimeLimitChanged)
                {
                    basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_JOB_TIME;
                    basicLimit.PerJobUserTimeLimit = this.jobUserTimeLimit;
                    this.jobUserTimeLimitChanged = false;
                }
                else
                {
                    basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_PRESERVE_JOB_TIME;
                }
            }

            if (this.priorityClass != 0)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_PRIORITY_CLASS;
                basicLimit.PriorityClass = this.priorityClass;
            }

            if (this.schedulingClass != 0)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_SCHEDULING_CLASS;
                basicLimit.SchedulingClass = this.schedulingClass;
            }

            if (this.affinity != IntPtr.Zero)
            {
                basicLimit.LimitFlags |= NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION.JOB_OBJECT_LIMIT_AFFINITY;
                basicLimit.Affinity = this.affinity;
            }

            extendedLimit.BasicLimitInformation = basicLimit;

            int extendedLimitLength = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr extendedLimitPtr = Marshal.AllocHGlobal(extendedLimitLength);

            try
            {
                Marshal.StructureToPtr(extendedLimit, extendedLimitPtr, false);

                bool success = NativeMethods.SetInformationJobObject(this.jobHandle, NativeMethods.JobObjectInfoClass.JobObjectExtendedLimitInformation, extendedLimitPtr, (uint)extendedLimitLength);

                if (success == false)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "SetInformationJobObject failed.");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(extendedLimitPtr);
            }
        }

        /// <summary>
        /// Queries the basic and IO accounting.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "QueryInformationJobObject", Justification = "Appropriate to suppress.")]
        private void QueryBasicAndIoAccounting()
        {
            NativeMethods.JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION accounting = new NativeMethods.JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION();
            accounting.BasicInfo = new NativeMethods.JOBOBJECT_BASIC_ACCOUNTING_INFORMATION();
            accounting.IoInfo = new NativeMethods.IO_COUNTERS();

            int accountingLength = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION));
            IntPtr accountingPtr = Marshal.AllocHGlobal(accountingLength);

            try
            {
                bool success = NativeMethods.QueryInformationJobObject(this.jobHandle, NativeMethods.JobObjectInfoClass.JobObjectBasicAndIoAccountingInformation, accountingPtr, (uint)accountingLength, IntPtr.Zero);

                if (success == false)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "QueryInformationJobObject failed.");
                }

                accounting = (NativeMethods.JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION)Marshal.PtrToStructure(accountingPtr, typeof(NativeMethods.JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION));

                this.userProcessorTime = (ulong)accounting.BasicInfo.TotalUserTime;
                this.kernelProcessorTime = (ulong)accounting.BasicInfo.TotalKernelTime;
                this.totalProcesses = (uint)accounting.BasicInfo.TotalProcesses;
                this.activeProcesses = (uint)accounting.BasicInfo.ActiveProcesses;
                this.totalTerminatedProcesses = (uint)accounting.BasicInfo.TotalTerminatedProcesses;

                this.ioReadBytes = (long)accounting.IoInfo.ReadTransferCount;
                this.ioWriteBytes = (long)accounting.IoInfo.WriteTransferCount;
                this.ioOtherBytes = (long)accounting.IoInfo.OtherTransferCount;
                this.ioReadOperationsCount = (long)accounting.IoInfo.ReadOperationCount;
                this.ioWriteOperationsCount = (long)accounting.IoInfo.WriteOperationCount;
                this.ioOtherOperationsCount = (long)accounting.IoInfo.OtherOperationCount;
            }
            finally
            {
                Marshal.FreeHGlobal(accountingPtr);
            }
        }

        /// <summary>
        /// Queries the extended limit information.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "QueryInformationJobObject", Justification = "Spelling is correct.")]
        private void QueryExtendedLimitInformation()
        {
            NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION extendedLimit;

            int extenedLimitLength = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr extendedLimitPtr = Marshal.AllocHGlobal(extenedLimitLength);

            try
            {
                bool success = NativeMethods.QueryInformationJobObject(this.jobHandle, NativeMethods.JobObjectInfoClass.JobObjectExtendedLimitInformation, extendedLimitPtr, (uint)extenedLimitLength, IntPtr.Zero);

                if (success == false)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "QueryInformationJobObject failed.");
                }

                extendedLimit = (NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION)Marshal.PtrToStructure(extendedLimitPtr, typeof(NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));

                this.peakJobMemory = (ulong)extendedLimit.PeakJobMemoryUsed;
                this.peakProcessMemory = (ulong)extendedLimit.PeakProcessMemoryUsed;
            }
            finally
            {
                Marshal.FreeHGlobal(extendedLimitPtr);
            }
        }
    }
}
