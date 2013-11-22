using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Tests.Rules
{
    [TestClass]
    public class TestMemoryRule
    {

        [TestMethod]
        public void AllowLessThenLimitMemory()
        {
            // Arrange
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.Memory;
            prisonRules.TotalPrivateMemoryLimitBytes = 100 * 1024 * 1024;
            prisonRules.PrisonHomePath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
@"
byte[] memory = new byte[50 * 1024 * 1024];

Random rnd = new Random();
rnd.NextBytes(memory);
", prison);

            Process process = prison.Execute(exe);

            process.WaitForExit();

            // Assert
            Assert.AreEqual(0, process.ExitCode);
        }

        [TestMethod]
        public void DenyExcesiveMemory()
        {
            // Arrange
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.Memory;
            prisonRules.TotalPrivateMemoryLimitBytes = 50 * 1024 * 1024;
            prisonRules.PrisonHomePath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
@"
byte[] memory = new byte[100 * 1024 * 1024];

Random rnd = new Random();
rnd.NextBytes(memory);
", prison);

            Process process = prison.Execute(exe);

            process.WaitForExit();

            // Assert
            Assert.AreNotEqual(0, process.ExitCode);
        }
    }
}
