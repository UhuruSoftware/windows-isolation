using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using System.Collections.Generic;
using Uhuru.Prison.Restrictions.Fakes;
using Uhuru.Prison.Restrictions;
using DiskQuotaTypeLibrary;

namespace Uhuru.Prison.FakesUnitTest
{
    [TestClass]
    public class TestDisk
    {
        [TestMethod]
        public void PrisonApplyDiskTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyDiskRuleFakes();
                
                long quotaSetTo = 0;
                ShimDisk.ShimDiskQuotaManager.SetDiskQuotaLimitStringStringInt64 = (WindowsUsername, Path, DiskQuotaBytes) => { quotaSetTo = DiskQuotaBytes; return; };
                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Disk;
                prisonRules.DiskQuotaBytes = 500; 
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                Assert.AreEqual(quotaSetTo, 500);
            }
        }

        [TestMethod]
        public void PrisonDestroyDiskTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyDiskRuleFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Disk;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonDestroyFakes();
                prison.Destroy();
            }
        }

        [TestMethod]
        public void PrisonReattachDiskTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyDiskRuleFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.Disk;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);
                prison.Reattach();
            }
        }

        [TestMethod]
        public void PrisonInitDiskTest()
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
        public void TestPrisonDiskListCellInstances()
        {
            using (ShimsContext.Create())
            {
                Prison prison = new Prison();
                prison.Tag = "uhtst";

                Dictionary<RuleType, RuleInstanceInfo[]> rules = Prison.ListCellInstances();
                Assert.IsTrue(rules[RuleType.Disk].Length == 0);
            }
        }
    }
}
