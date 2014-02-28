// -----------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.Prison.Utilities.WindowsJobObjects
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Security;

    /// <summary>
    /// Job Objects Windows API native methods.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        /// <summary>
        /// Used for calling the Win API
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "Appropriate to suppress.")]
        internal enum JobObjectInfoClass
        {
            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_ACCOUNTING_INFORMATION structure.
            /// </summary>
            JobObjectBasicAccountingInformation = 1,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION structure.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Io", Justification = "Appropriate to suppress.")]
            JobObjectBasicAndIoAccountingInformation = 8,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_LIMIT_INFORMATION structure.
            /// </summary>
            JobObjectBasicLimitInformation = 2,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_PROCESS_ID_LIST structure.
            /// </summary>
            JobObjectBasicProcessIdList = 3,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_BASIC_UI_RESTRICTIONS structure.
            /// </summary>
            JobObjectBasicUIRestrictions = 4,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_END_OF_JOB_TIME_INFORMATION structure.
            /// </summary>
            JobObjectEndOfJobTimeInformation = 6,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_EXTENDED_LIMIT_INFORMATION structure.
            /// </summary>
            JobObjectExtendedLimitInformation = 9,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_ASSOCIATE_COMPLETION_PORT structure.
            /// </summary>
            JobObjectAssociateCompletionPortInformation = 7,

            /// <summary>
            /// The lpJobObjectInfo parameter is a pointer to a JOBOBJECT_CPU_RATE_CONTROL_INFORMATION structure.
            /// </summary>
            JobObjectCpuRateControlInformation = 15,

        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern JobObjectHandle CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsProcessInJob(IntPtr Process, JobObjectHandle hJob, [MarshalAs(UnmanagedType.Bool)] out bool Result);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetInformationJobObject(JobObjectHandle hJob, JobObjectInfoClass JobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryInformationJobObject(JobObjectHandle hJob, JobObjectInfoClass JobObjectInformationClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength, IntPtr lpReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AssignProcessToJobObject(JobObjectHandle hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateJobObject(JobObjectHandle hJob, uint uExitCode);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern JobObjectHandle OpenJobObject(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hHandle);

        /// <summary>
        /// The SECURITY_ATTRIBUTES structure contains the security descriptor for an object and specifies whether the handle retrieved by specifying this structure is inheritable.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Preserve WinAPI naming.")]
        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            /// <summary>
            /// Length of the structure.
            /// </summary>
            public int Length;

            /// <summary>
            /// Security Descriptor
            /// </summary>
            public IntPtr SecurityDescriptor;

            /// <summary>
            /// Child process inheritance.
            /// </summary>
            public int InheritHandle;
        }

        /// <summary>
        /// Contains basic and extended limit information for a job object.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Preserve WinAPI naming.")]
        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            /// <summary>
            /// JOB_OBJECT_LIMIT_ACTIVE_PROCESS Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 0x00000008;

            /// <summary>
            /// JOB_OBJECT_LIMIT_AFFINITY Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_AFFINITY = 0x00000010;

            /// <summary>
            /// JOB_OBJECT_LIMIT_BREAKAWAY_OK Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x00000800;

            /// <summary>
            /// JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION = 0x00000400;

            /// <summary>
            /// JOB_OBJECT_LIMIT_JOB_MEMORY Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_JOB_MEMORY = 0x00000200;

            /// <summary>
            /// JOB_OBJECT_LIMIT_JOB_TIME Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_JOB_TIME = 0x00000004;

            /// <summary>
            /// JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

            /// <summary>
            /// JOB_OBJECT_LIMIT_PRESERVE_JOB_TIME Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_PRESERVE_JOB_TIME = 0x00000040;

            /// <summary>
            /// JOB_OBJECT_LIMIT_PRIORITY_CLASS Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_PRIORITY_CLASS = 0x00000020;

            /// <summary>
            /// JOB_OBJECT_LIMIT_PROCESS_MEMORY Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x00000100;

            /// <summary>
            /// JOB_OBJECT_LIMIT_PROCESS_TIME Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_PROCESS_TIME = 0x00000002;

            /// <summary>
            /// JOB_OBJECT_LIMIT_SCHEDULING_CLASS Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_SCHEDULING_CLASS = 0x00000080;

            /// <summary>
            /// JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK = 0x00001000;

            /// <summary>
            /// JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_SUBSET_AFFINITY = 0x00004000;

            /// <summary>
            /// JOB_OBJECT_LIMIT_WORKINGSET Windows API constant.
            /// </summary>
            public const uint JOB_OBJECT_LIMIT_WORKINGSET = 0x00000001;

            /// <summary>
            /// Per process user time limit.
            /// </summary>
            public long PerProcessUserTimeLimit;

            /// <summary>
            /// Per job user time limit.
            /// </summary>
            public long PerJobUserTimeLimit;

            /// <summary>
            /// Limit flags.
            /// </summary>
            public uint LimitFlags;

            /// <summary>
            /// Minimum working set size.
            /// </summary>
            public IntPtr MinimumWorkingSetSize;

            /// <summary>
            /// Maximum working set size.
            /// </summary>
            public IntPtr MaximumWorkingSetSize;

            /// <summary>
            /// Active process limit.
            /// </summary>
            public uint ActiveProcessLimit;

            /// <summary>
            /// Processor affinity.
            /// </summary>
            public IntPtr Affinity;

            /// <summary>
            /// Priority class.
            /// </summary>
            public uint PriorityClass;

            /// <summary>
            /// Scheduling class.
            /// </summary>
            public uint SchedulingClass;
        }

        /// <summary>
        /// JOBOBJECT_BASIC_UI_RESTRICTIONS Windows API structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_BASIC_UI_RESTRICTIONS
        {
            /// <summary>
            /// UI Restrictions class.
            /// </summary>
            public uint UIRestrictionsClass;
        }

        /// <summary>
        /// JOBOBJECT_CPU_RATE_CONTROL_INFORMATION Windows API structure.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Preserve WinAPI naming.")]
        [StructLayout(LayoutKind.Explicit)]
        internal struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
        {
            public enum CpuRateControlFlags
            {
                /// <summary>
                /// This flag enables the job's CPU rate to be controlled based on weight or hard cap. 
                /// It must be set if JOB_OBJECT_CPU_RATE_CONTROL_WEIGHT_BASED or JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP is set.
                /// <remarks>Source: http://msdn.microsoft.com/en-us/library/windows/desktop/hh448384(v=vs.85).aspx </remarks>
                /// </summary>
                JOB_OBJECT_CPU_RATE_CONTROL_ENABLE = 1,

                /// <summary>
                /// The job's CPU rate is calculated based on its relative weight to the weight of other jobs.
                /// If this flag is set, the Weight member contains more information. If this flag is clear, the CpuRate member contains more information.
                /// <remarks>Source: http://msdn.microsoft.com/en-us/library/windows/desktop/hh448384(v=vs.85).aspx </remarks>
                /// </summary>
                JOB_OBJECT_CPU_RATE_CONTROL_WEIGHT_BASED = 2,

                /// <summary>
                /// The job's CPU rate is a hard limit. After the job reaches its CPU cycle limit for the current scheduling interval, no threads associated with the job will run until the next interval.
                /// <remarks>Source: http://msdn.microsoft.com/en-us/library/windows/desktop/hh448384(v=vs.85).aspx </remarks>
                /// </summary>
                JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP = 4,

                /// <summary>
                /// The system will send a JOB_OBJECT_MSG_NOTIFICATION_LIMIT message to the job's I/O completion port if the job exceeds the CPU rate control limits specified in this structure within the tolerance specified in the JOBOBJECT_NOTIFICATION_LIMIT_INFORMATION structure.
                /// <remarks>Source: http://msdn.microsoft.com/en-us/library/windows/desktop/hh448384(v=vs.85).aspx </remarks>
                /// </summary>
                JOB_OBJECT_CPU_RATE_CONTROL_NOTIFY = 8
            }

            /// <summary>
            /// Control Flags.
            /// </summary>
            [FieldOffset(0)]
            public UInt32 ControlFlags;

            /// <summary>
            /// CPU rate.
            /// </summary>
            [FieldOffset(4)]
            public UInt32 CpuRate;

            /// <summary>
            /// CPU rate weight.
            /// </summary>
            [FieldOffset(4)]
            public UInt32 Weight;
        }

        /// <summary>
        /// JOBOBJECT_EXTENDED_LIMIT_INFORMATION Windows API structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            /// <summary>
            /// BasicLimitInformation Windows API structure member.
            /// </summary>
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;

            /// <summary>
            /// IoInfo Windows API structure member.
            /// </summary>
            public IO_COUNTERS IoInfo;

            /// <summary>
            /// ProcessMemoryLimit Windows API structure member.
            /// </summary>
            public IntPtr ProcessMemoryLimit;

            /// <summary>
            /// JobMemoryLimit Windows API structure member.
            /// </summary>
            public IntPtr JobMemoryLimit;

            /// <summary>
            /// PeakProcessMemoryUsed Windows API structure member.
            /// </summary>
            public IntPtr PeakProcessMemoryUsed;

            /// <summary>
            /// PeakJobMemoryUsed Windows API structure member.
            /// </summary>
            public IntPtr PeakJobMemoryUsed;
        }

        /// <summary>
        /// IO_COUNTERS Windows API structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct IO_COUNTERS
        {
            /// <summary>
            /// ReadOperationCount Windows API structure member.
            /// </summary>
            public ulong ReadOperationCount;

            /// <summary>
            /// WriteOperationCount Windows API structure member.
            /// </summary>
            public ulong WriteOperationCount;

            /// <summary>
            /// OtherOperationCount Windows API structure member.
            /// </summary>
            public ulong OtherOperationCount;

            /// <summary>
            /// ReadTransferCount Windows API structure member.
            /// </summary>
            public ulong ReadTransferCount;

            /// <summary>
            /// WriteTransferCount Windows API structure member.
            /// </summary>
            public ulong WriteTransferCount;

            /// <summary>
            /// OtherTransferCount Windows API structure member.
            /// </summary>
            public ulong OtherTransferCount;
        }

        /// <summary>
        /// JOBOBJECT_BASIC_ACCOUNTING_INFORMATION Windows API structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
        {
            /// <summary>
            /// TotalUserTime Windows API structure member.
            /// </summary>
            public ulong TotalUserTime;

            /// <summary>
            /// TotalKernelTime Windows API structure member.
            /// </summary>
            public ulong TotalKernelTime;

            /// <summary>
            /// ThisPeriodTotalUserTime Windows API structure member.
            /// </summary>
            public ulong ThisPeriodTotalUserTime;

            /// <summary>
            /// ThisPeriodTotalKernelTime Windows API structure member.
            /// </summary>
            public ulong ThisPeriodTotalKernelTime;

            /// <summary>
            /// TotalPageFaultCount Windows API structure member.
            /// </summary>
            public uint TotalPageFaultCount;

            /// <summary>
            /// TotalProcesses Windows API structure member.
            /// </summary>
            public uint TotalProcesses;

            /// <summary>
            /// ActiveProcesses Windows API structure member.
            /// </summary>
            public uint ActiveProcesses;

            /// <summary>
            /// TotalTerminatedProcesses Windows API structure member.
            /// </summary>
            public uint TotalTerminatedProcesses;
        }

        /// <summary>
        /// JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION Windows API structure.
        /// </summary>
        internal struct JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION
        {
            /// <summary>
            /// BasicInfo Windows API structure member.
            /// </summary>
            public JOBOBJECT_BASIC_ACCOUNTING_INFORMATION BasicInfo;

            /// <summary>
            /// IoInfo Windows API structure member.
            /// </summary>
            public IO_COUNTERS IoInfo;
        }

        /// <summary>
        /// JOBOBJECT_BASIC_PROCESS_ID_LIST Windows API structure.
        /// </summary>
        internal struct JOBOBJECT_BASIC_PROCESS_ID_LIST
        {
            /// <summary>
            /// The maximum number of processes that are allocated when querying Windows API. 
            /// </summary>
            public const uint MaxProcessListLength = 200;

            /// <summary>
            /// NumberOfAssignedProcesses Windows API structure member.
            /// </summary>
            public uint NumberOfAssignedProcesses;

            /// <summary>
            /// NumberOfProcessIdsInList Windows API structure member.
            /// </summary>
            public uint NumberOfProcessIdsInList;

            /// <summary>
            /// ProcessIdList Windows API structure member.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MaxProcessListLength)]
            public IntPtr[] ProcessIdList;
        }
    }
}
