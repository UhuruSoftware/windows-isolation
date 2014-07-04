using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using System.Collections.Generic;
using System.Management.Fakes;
using System.Management;
using Uhuru.Prison.Restrictions.Fakes;

namespace Uhuru.Prison.FakesUnitTest
{
    [TestClass]
    public class TestNetwork
    {
        [TestMethod]
        public void PrisonApplyNetworkTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyNetworkRuleFakes();

                ManagementObject mobj = null;

                ShimManagementObject.AllInstances.Put = 
                    (@this) => 
                    {
                        mobj  = @this;
                        return new ShimManagementPath(); 
                    };

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Network;
                prisonRules.NetworkOutboundRateLimitBitsPerSecond = 500;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                Assert.AreEqual(mobj["ThrottleRateAction"].ToString(), 500.ToString());
            }
        }

        [TestMethod]
        public void PrisonApplyNetworkAppTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyNetworkRuleFakes();

                ManagementObject mobj = null;

                ShimManagementObject.AllInstances.Put =
                    (@this) =>
                    {
                        mobj = @this;
                        return new ShimManagementPath();
                    };

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Network;
                prisonRules.UrlPortAccess = 56444;
                prisonRules.AppPortOutboundRateLimitBitsPerSecond = 500;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                Assert.AreEqual(mobj["ThrottleRateAction"].ToString(), 500.ToString());
                Assert.IsTrue(mobj["URIMatchCondition"].ToString().Contains(56444.ToString()));
            }
        }

        [TestMethod]
        public void PrisonDestroyNetworkTest()
        {
            
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyNetworkRuleFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Network;
                prisonRules.NetworkOutboundRateLimitBitsPerSecond = 500;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonDestroyFakes();
                ShimNetwork.RemoveOutboundThrottlePolicyString = (username) => { return; };
                prison.Destroy();
            }
        }

        [TestMethod]
        public void PrisonReattachNetworkTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyNetworkRuleFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Network;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);
                prison.Reattach();
            }
        }

        [TestMethod]
        public void PrisonInitNetworkTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.InitFilesystemRuleFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";

                Prison.Init();
            }
        }

        [TestMethod]
        public void TestPrisonNetworkListCellInstances()
        {
            using (ShimsContext.Create())
            {
                string username = "prison_user";
                PrisonTestsHelper.ListNetworkRuleFakes(username);

                Prison prison = new Prison();
                prison.Tag = "uhtst";

                Dictionary<RuleType, RuleInstanceInfo[]> rules = Prison.ListCellInstances();
                Assert.AreEqual(username, rules[RuleType.Network][0].Name);
            }
        }
    }
}
