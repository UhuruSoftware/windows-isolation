using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Tests
{
    [TestClass]
    public class TestDiskRule
    {
        [TestMethod]
        public void AllowLessThenLimitDisk()
        {
            // Arrange
            Prison.Init();
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.Disk;
            prisonRules.DiskQuotaBytes = 100 * 1024 * 1024;
            prisonRules.PrisonHomePath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
@"
for (int size = 1; size < 50; size++)
{{
    byte[] content = new byte[1024 * 1024];

    File.AppendAllText(Guid.NewGuid().ToString(""N""), ASCIIEncoding.ASCII.GetString(content));
}}", prison);
            
            Process process = prison.Execute(exe);

            process.WaitForExit();

            // Assert
            Assert.AreEqual(0, process.ExitCode);
        }

        [TestMethod]
        public void DenyExcesiveDiskUsage()
        {
            // Arrange
            Prison.Init();
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.Disk;
            prisonRules.DiskQuotaBytes = 50 * 1024 * 1024;
            prisonRules.PrisonHomePath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
@"
for (int size = 1; size < 100; size++)
{{
    byte[] content = new byte[1024 * 1024];

    File.AppendAllText(Guid.NewGuid().ToString(""N""), ASCIIEncoding.ASCII.GetString(content));
}}", prison);

            Process process = prison.Execute(exe);

            process.WaitForExit();

            // Assert
            Assert.AreNotEqual(0, process.ExitCode);
        }
    }
}
