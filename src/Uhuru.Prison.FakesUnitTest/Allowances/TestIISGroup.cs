using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using System.Collections.Generic;

namespace Uhuru.Prison.FakesUnitTest
{
    [TestClass]
    public class TestIISGroup
    {
        [TestMethod]
        public void PrisonApplyIISGroupTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyIISGroupFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.IISGroup;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);
            }
        }

        [TestMethod]
        public void PrisonDestroyIISGroupTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyIISGroupFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.IISGroup;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonDestroyFakes();
                prison.Destroy();
            }
        }

        [TestMethod]
        public void PrisonReattachIISGroupTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyIISGroupFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.IISGroup;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);
                prison.Reattach();
            }
        }

        [TestMethod]
        public void PrisonInitIISGroupTest()
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
        public void TestPrisonIISGroupListCellInstances()
        {
            using (ShimsContext.Create())
            {
                Prison prison = new Prison();
                prison.Tag = "uhtst";

                Dictionary<RuleType, RuleInstanceInfo[]> rules = Prison.ListCellInstances();
                Assert.IsTrue(rules[RuleType.IISGroup].Length == 0);
            }
        }
    }
}
