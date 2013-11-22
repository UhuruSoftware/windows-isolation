using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison
{
    public class PrisonRules
    {
        public string Id
        {
            get;
            set;
        }

        public long TotalPrivateMemoryLimitBytes
        {
            get;
            set;
        }

        public int RunningProcessesLimit
        {
            get;
            set;
        }

        /// <summary>
        /// The space usage quota for VolumeRootPath.
        /// Use -1 to disable disk quota.
        /// </summary>
        public long DiskQuotaBytes
        {
            get;
            set;
        }


        /// <summary>
        /// The path in the disk volume to apply quota on.
        /// Ex. "C:\dir" for volume "C:\"
        /// </summary>
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
        public int UrlPortAccess
        {
            get;
            set;
        }


        public RuleType CellType
        {
            get;
            set;
        }
    }
}
