using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using System.Collections.Generic;
using Uhuru.Prison.Restrictions.Fakes;

namespace Uhuru.Prison.FakesUnitTest
{
    [TestClass]
    public class TestWindowStation
    {
        [TestMethod]
        public void PrisonApplyWindowStationTest()
        {
            using (ShimsContext.Create())
            {
                int winStationPtr = 2658;

                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyWindowStationRuleFakes(winStationPtr);

                string username = null;
                ShimWindowStation.NativeOpenWindowStationString = (user) => { username = user; return new IntPtr(winStationPtr); };
                ShimWindowStation.NativeCreateWindowStationString = (user) => { username = user; return new IntPtr(winStationPtr); };

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.WindowStation;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                Assert.AreEqual(prison.desktopName, string.Format(@"{0}\Default", username));
            }
        }

        [TestMethod]
        public void PrisonDestroyWindowStationTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyWindowStationRuleFakes(256);

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.WindowStation;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonDestroyFakes();
                prison.Destroy();
            }
        }

        [TestMethod]
        public void PrisonReattachWindowStationTest()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                PrisonTestsHelper.ApplyWindowStationRuleFakes(2658);

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.CellType |= RuleType.WindowStation;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);
                prison.Reattach();
            }
        }

        [TestMethod]
        public void PrisonInitWindowStationTest()
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
        public void TestPrisonWindowStationkListCellInstances()
        {
            using (ShimsContext.Create())
            {
                var fakedStations = new List<string>() { "WinSta0", "WinStaLalala" };

                PrisonTestsHelper.ListWindowStationRuleFakes(fakedStations);

                Prison prison = new Prison();
                prison.Tag = "uhtst";

                Dictionary<RuleType, RuleInstanceInfo[]> rules = Prison.ListCellInstances();

                Assert.AreEqual(fakedStations.Count, rules[RuleType.WindowStation].Length);

                foreach(var wstation in rules[RuleType.WindowStation])
                {
                    Assert.IsTrue(fakedStations.Contains(wstation.Name));
                }
            }
        }
    }
}
