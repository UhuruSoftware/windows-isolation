using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using Uhuru.Prison.Utilities.Fakes;
using System.Collections.Generic;

namespace Uhuru.Prison.FakesUnitTest
{
    [TestClass]
    public class TestFilesystem
    {
        [TestMethod]
        public void PrisonApplyFilesystemTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyFilesystemFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Filesystem;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);
            }
        }

        [TestMethod]
        public void PrisonDestroyFilesystemTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyFilesystemFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Filesystem;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonDestroyFakes();
                prison.Destroy();
            }
        }

        [TestMethod]
        public void PrisonReattachFilesystemTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyFilesystemFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Filesystem;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);
                prison.Reattach();
            }
        }

        [TestMethod]
        public void PrisonInitFilesystemTest()
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
        public void TestPrisonFilesystemListCellInstances()
        {
            using (ShimsContext.Create())
            {
                Prison prison = new Prison();
                prison.Tag = "uhtst";

                Dictionary<RuleType, RuleInstanceInfo[]> rules = Prison.ListCellInstances();
                Assert.IsTrue(rules[RuleType.Filesystem].Length == 0);
            }
        }
    }
}
