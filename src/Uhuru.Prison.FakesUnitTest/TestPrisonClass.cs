using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using Uhuru.Prison.Utilities.Fakes;
using Uhuru.Prison.Fakes;
using System.Runtime.Serialization.Fakes;
using System.Collections.Generic;

namespace Uhuru.Prison.FakesUnitTest
{
    [TestClass]
    public class TestPrisonClass
    {
        [TestMethod]
        public void TestLockdown()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();
                string createdUser = null;
                string userProfileDestination = null;
                bool saveWasInvoked = false;
                ShimWindowsUsersAndGroups.CreateUserStringString = (username, password) => { createdUser = username;  return; };
                ShimPrison.AllInstances.ChangeRegistryUserProfileString = (pris, destination) => { userProfileDestination = destination; return; };
                ShimDataContractSerializer.AllInstances.WriteObjectContentXmlWriterObject = (data, writeStream, fakePrison) => { saveWasInvoked = true; return; };

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                Assert.AreEqual(createdUser, prison.User.Username);
                Assert.IsTrue(createdUser.Contains(prison.Tag));

                // The user profile has to be moved in the prison home dir
                Assert.IsTrue(userProfileDestination.Contains(prisonRules.PrisonHomePath));

                Assert.IsTrue(saveWasInvoked);
            }
        }

        [TestMethod]
        public void TestDestroy()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonDestroyFakes();

                prison.Destroy();
            }

        }

        [TestMethod]
        public void TestLoad()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonLoadFakes(prison.ID);
                Prison[] prisons = Prison.Load();

                Assert.AreEqual(prisons.Length, 1);
                foreach (var prisonItem in prisons)
                {
                    Assert.IsTrue(prisonItem.ID == prison.ID);
                }
            }
        }

        [TestMethod]
        public void TestLoadPrisonAndAttach()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonLoadFakes(prison.ID);
                Prison prisonLoaded = Prison.LoadPrisonAndAttach(prison.ID);

                Assert.IsTrue(prisonLoaded != null);
                Assert.IsTrue(prisonLoaded.ID == prison.ID);
            }
        }

        [TestMethod]
        public void TestLoadPrisonNoAttach()
        {
            using (ShimsContext.Create())
            {
                PrisonTestsHelper.PrisonLockdownFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";
                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                PrisonTestsHelper.PrisonLoadFakes(prison.ID);
                Prison prisonLoaded = Prison.LoadPrisonNoAttach(prison.ID);

                Assert.IsTrue(prisonLoaded != null);
                Assert.IsTrue(prisonLoaded.ID == prison.ID);
            }
        }
    }
}
