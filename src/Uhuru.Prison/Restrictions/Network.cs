using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Restrictions
{
    class Network : Rule
    {
        public override void Apply(Prison prison)
        {
            Network.CreateOutboundThrottlePolicy(prison.User.Username, prison.User.Username, prison.Rules.NetworkOutboundRateLimitBitsPerSecond);

            if (prison.Rules.UrlPortAccess > 0)
            {
                Network.RemoveOutboundThrottlePolicy(PrisonUser.GlobalPrefix + PrisonUser.Separator + prison.Rules.UrlPortAccess.ToString());
                Network.CreateOutboundThrottlePolicy(PrisonUser.GlobalPrefix + PrisonUser.Separator + prison.Rules.UrlPortAccess.ToString(), prison.Rules.UrlPortAccess, prison.Rules.AppPortOutboundRateLimitBitsPerSecond);
            }
        }

        public override void Destroy(Prison prison)
        {
            Network.RemoveOutboundThrottlePolicy(prison.User.Username);
            Network.RemoveOutboundThrottlePolicy(PrisonUser.GlobalPrefix + PrisonUser.Separator + prison.Rules.UrlPortAccess.ToString());
        }

        /// <summary>
        /// Sets the limit for the upload network data rate. This limit is applied for the specified user.
        /// This method is not reentrant. Remove the policy first after creating it again.
        /// </summary>
        private static void CreateOutboundThrottlePolicy(string ruleName, string windowsUsername, long bitsPerSecond)
        {
            var StandardCimv2 = new ManagementScope(@"root\StandardCimv2");

            using (ManagementClass netqos = new ManagementClass("MSFT_NetQosPolicySettingData"))
            {
                netqos.Scope = StandardCimv2;

                using (ManagementObject newInstance = netqos.CreateInstance())
                {
                    newInstance["Name"] = ruleName;
                    newInstance["UserMatchCondition"] = windowsUsername;

                    // ThrottleRateAction is in bytesPerSecond according to the WMI docs.
                    // Acctualy the units are bits per second, as documented in the PowerShell cmdlet counterpart.
                    newInstance["ThrottleRateAction"] = bitsPerSecond;

                    newInstance.Put();
                }
            }
        }

        /// <summary>
        /// Sets the limit for the upload network data rate. This limit is applied for a specific server URL passing through HTTP.sys.
        /// This rules are applicable to IIS, IIS WHC and IIS Express. This goes hand in hand with URL Acls.
        /// This method is not reentrant. Remove the policy first after creating it again.
        /// </summary>
        private static void CreateOutboundThrottlePolicy(string ruleName, int urlPort, long bitsPerSecond)
        {
            var StandardCimv2 = new ManagementScope(@"root\StandardCimv2");

            using (ManagementClass netqos = new ManagementClass("MSFT_NetQosPolicySettingData"))
            {
                netqos.Scope = StandardCimv2;

                using (ManagementObject newInstance = netqos.CreateInstance())
                {
                    newInstance["Name"] = ruleName;
                    newInstance["URIMatchCondition"] = String.Format("http://*:{0}/", urlPort);
                    newInstance["URIRecursiveMatchCondition"] = true;

                    // ThrottleRateAction is in bytesPerSecond according to the WMI docs.
                    // Acctualy the units are bits per second, as documented in the PowerShell cmdlet counterpart.
                    newInstance["ThrottleRateAction"] = bitsPerSecond;

                    newInstance.Put();
                }
            }
        }

        private static void RemoveOutboundThrottlePolicy(string ruleName)
        {
            var wql = string.Format("SELECT * FROM MSFT_NetQosPolicySettingData WHERE Name = \"{0}\"", ruleName);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\StandardCimv2", wql))
            {
                // should only iterate once
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    queryObj.Delete();
                    queryObj.Dispose();
                }
            }
        }

        private static ManagementObjectCollection GetThrottlePolicies()
        {
            var wql = "SELECT * FROM MSFT_NetQosPolicySettingData";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\StandardCimv2", wql))
            {
                return searcher.Get();
            }
        }

        public override RuleInstanceInfo[] List()
        {
            List<RuleInstanceInfo> result = new List<RuleInstanceInfo>();

            foreach (ManagementObject policy in GetThrottlePolicies())
            {
                if (!string.IsNullOrWhiteSpace(policy["Name"] as string) && policy["Name"].ToString().StartsWith(PrisonUser.GlobalPrefix + PrisonUser.Separator))
                {
                    string info = string.Format("{0} bps; match: {1}",
                        policy["ThrottleRateAction"],
                        policy["URIMatchCondition"] != null ? policy["URIMatchCondition"] : (policy["UserMatchCondition"] != null ? policy["UserMatchCondition"] : string.Empty)
                        );

                    result.Add(new RuleInstanceInfo()
                    {
                        Name = policy["Name"].ToString(),
                        Info = info
                    });
                }
            }

            return result.ToArray();
        }

        public override void Init()
        {
        }

        public override RuleType GetFlag()
        {
            return RuleType.Network;
        }

        public override void Recover(Prison prison)
        {
        }
    }
}
