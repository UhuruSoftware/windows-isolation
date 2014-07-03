using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using Uhuru.Prison.Utilities.Fakes;
using System.Collections.Generic;

namespace Uhuru.Prison.FakesUnitTest
{
    [TestClass]
    public class TestHttpsys
    {
        [TestMethod]
        public void PrisonApplyHttpsysTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyHttpsysFakes();
                string addPortCommand = null;
                ShimCommand.ExecuteCommandString = (command) => { addPortCommand = command; return 0; }; 

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Httpsys;
                prisonRules.UrlPortAccess = 5400;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                Assert.IsTrue(addPortCommand.Contains(prisonRules.UrlPortAccess.ToString()));
            }
        }

        [TestMethod]
        public void PrisonDestroyHttpsysTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyHttpsysFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Httpsys;
                prisonRules.UrlPortAccess = 5400;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonDestroyFakes();
                PrisonTestsHelper.HttpsysRemovePortAccessFakes();
                prison.Destroy();
            }
        }

        [TestMethod]
        public void PrisonReattachHttpsysTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyHttpsysFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Httpsys;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);              
                prison.Reattach();
            }
        }

        [TestMethod]
        public void PrisonInitHttpsysTest()
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
        public void TestPrisonHttpsysListCellInstances()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.ListHttpsysRuleFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";

                Dictionary<RuleType, RuleInstanceInfo[]> rules = Prison.ListCellInstances();

                Assert.AreEqual(1, rules[RuleType.Httpsys].Length);

                foreach (var url in rules[RuleType.Httpsys])
                {
                    Assert.IsTrue(url.Name.Contains(prison.Tag));
                }
            }
        }
    }
}
