using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison
{
    [DataContract]
    public class PrisonRules
    {
        [DataMember]
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// The total amount of commit memory available to the processes in the Prison.
        /// </summary>
        [DataMember]
        public long TotalPrivateMemoryLimitBytes
        {
            get;
            set;
        }

        /// <summary>
        /// The total amount of CPU (as a percentage) available to the processes in the Prison.
        /// </summary>
        [DataMember]
        public long CPUPercentageLimit
        {
            get;
            set;
        }

        /// <summary>
        /// The total number of processes that can run at the same time 
        /// in the Prison.
        /// </summary>
        [DataMember]
        public int ActiveProcessesLimit
        {
            get;
            set;
        }

        /// <summary>
        /// The priority class for the job object.
        /// </summary>
        [DataMember]
        public ProcessPriorityClass? PriorityClass
        {
            get;
            set;
        }

        /// <summary>
        /// The space usage quota for VolumeRootPath.
        /// Use -1 to disable disk quota.
        /// </summary>
        [DataMember]
        public long DiskQuotaBytes
        {
            get;
            set;
        }


        /// <summary>
        /// The path in the disk volume to apply quota on.
        /// Ex. "C:\dir" for volume "C:\"
        /// </summary>
        [DataMember]
        public string PrisonHomePath
        {
            get;
            set;
        }

        /// <summary>
        /// The limit for network data upload rate in bits per second.
        /// Policy not enforced for local traffic.
        /// Use -1 to disable network throttling. NetworkOutboundRateLimitBitsPerSecond
        /// </summary>
        [DataMember]
        public long NetworkOutboundRateLimitBitsPerSecond
        {
            get;
            set;
        }

        /// <summary>
        /// The limit for network data upload rate in bits per second.
        /// Policy not enforced for local traffic.
        /// Use -1 to disable network throttling. NetworkOutboundRateLimitBitsPerSecond
        /// </summary>
        [DataMember]
        public long AppPortOutboundRateLimitBitsPerSecond
        {
            get;
            set;
        }

        /// <summary>
        /// Allows the prisson's user to bind to URLs with the specified port.
        /// URL bindings are needed for HTTP.sys access, e.g. IIS, IIS HWC, IIS Express.
        /// Use 0 to disable HTTP.sys port access.
        /// </summary>
        [DataMember]
        public int UrlPortAccess
        {
            get;
            set;
        }

        [DataMember]
        public RuleType CellType
        {
            get;
            set;
        }
    }
}
