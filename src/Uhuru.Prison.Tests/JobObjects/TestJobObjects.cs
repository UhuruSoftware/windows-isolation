using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Tests.JobObjects
{
    [TestClass]
    public class TestJobObjects
    {
        [TestMethod]
        public void TestSimpleEcho()
        {
            // Arrange
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.None;

            prison.Lockdown(prisonRules);

            // Act
            Process process = prison.Execute(
                @"c:\windows\system32\cmd.exe",
                @"/c echo test");

            // Assert
            Assert.AreNotEqual(0, process.Id);
        }

        [TestMethod]
        public void TestMultipleEcho()
        {
            // Arrange
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.None;
            prisonRules.PrisonHomePath = String.Format(@"c:\prison_tests\{0}", prison.ID);

            prison.Lockdown(prisonRules);

            // Act
            Process process1 = prison.Execute(
                @"c:\windows\system32\cmd.exe",
                @"/c echo test");

            Process process2 = prison.Execute(
                @"c:\windows\system32\cmd.exe",
                @"/c echo test");

            // Assert
            Assert.AreNotEqual(0, process1.Id);
            Assert.AreNotEqual(0, process2.Id);

            prison.Destroy();
        }

        [TestMethod]
        public void TestExitCode()
        {
            // Arrange
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.None;
            prisonRules.CellType |= RuleType.Filesystem;

            prisonRules.PrisonHomePath = String.Format(@"c:\prison_tests\{0}", prison.ID);

            prison.Lockdown(prisonRules);

            // Act
            Process process = prison.Execute(
                @"c:\windows\system32\cmd.exe",
                @"/c exit 667");

            process.WaitForExit();

            prison.Destroy();

            // Assert
            Assert.AreEqual(667, process.ExitCode);
        }
    }
}
