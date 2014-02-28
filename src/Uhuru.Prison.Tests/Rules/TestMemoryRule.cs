using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        [TestMethod]
        public void StopForkBombs()
        {
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.Memory;
            // prisonRules.CellType = RuleType.WindowStation;
            prisonRules.CPUPercentageLimit = 2;
            prisonRules.TotalPrivateMemoryLimitBytes = 50 * 1024 * 1024;
            prisonRules.PrisonHomePath = @"c:\prison_tests\p7";
            prisonRules.ActiveProcessesLimit = 5;

            prison.Lockdown(prisonRules);

            Process process = prison.Execute("", "cmd /c  for /L %n in (1,0,10) do (  start cmd /k echo 32  )");

            // Wait for the bomb to explode
            while (true)
            {
                if (prison.JobObject.ActiveProcesses >= 4) break;
                Thread.Sleep(100);
            }

            Thread.Sleep(500);

            Assert.IsTrue(prison.JobObject.ActiveProcesses < 6);

            prison.Destroy();
        }

        [TestMethod]
        // Currently not working
        public void LimitPagedPool()
        {
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            // prisonRules.CellType = RuleType.WindowStation;
            prisonRules.CellType = RuleType.Memory;
            prisonRules.TotalPrivateMemoryLimitBytes = 50 * 1024 * 1024;
            prisonRules.PrisonHomePath = @"c:\prison_tests\p9";
            prisonRules.ActiveProcessesLimit = 5;

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
@"
    string MailslotName = @""\\.\mailslot\sterssmailslot"";

    var hMailslotA = CreateMailslot(MailslotName, 0, MAILSLOT_WAIT_FOREVER, IntPtr.Zero);

    var hMailslot = CreateFile(MailslotName, FileDesiredAccess.GENERIC_WRITE, FileShareMode.FILE_SHARE_READ, IntPtr.Zero, FileCreationDisposition.OPEN_EXISTING, 0, IntPtr.Zero);

    int cbBytesWritten;
    byte[] bMessage = Encoding.Unicode.GetBytes(""Hello mailslot! Still alive?"");

    while (true)
    {
        WriteFile(hMailslot, bMessage, bMessage.Length, out cbBytesWritten, IntPtr.Zero);
    }

return 0;

}

        [Flags]
        enum FileDesiredAccess : uint
        {
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000
        }

        [Flags]
        enum FileShareMode : uint
        {
            Zero = 0x00000000,  // No sharing
            FILE_SHARE_DELETE = 0x00000004,
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002
        }

        enum FileCreationDisposition : uint
        {
            CREATE_NEW = 1,
            CREATE_ALWAYS = 2,
            OPEN_EXISTING = 3,
            OPEN_ALWAYS = 4,
            TRUNCATE_EXISTING = 5
        }

        const int MAILSLOT_WAIT_FOREVER = -1;
        const int MAILSLOT_NO_MESSAGE = -1;

        [DllImport(""kernel32.dll"", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr CreateMailslot(string mailslotName,
            uint nMaxMessageSize, int lReadTimeout,
            IntPtr securityAttributes);

        [DllImport(""kernel32.dll"", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr CreateFile(string fileName,
            FileDesiredAccess desiredAccess, FileShareMode shareMode,
            IntPtr securityAttributes,
            FileCreationDisposition creationDisposition,
            int flagsAndAttributes, IntPtr hTemplateFile);

        [DllImport(""kernel32.dll"", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool WriteFile(IntPtr handle,
            byte[] bytes, int numBytesToWrite, out int numBytesWritten,
            IntPtr overlapped);

private static int Dummy()
{

", prison);

            Process process = prison.Execute(exe);

            long lastVal = 0;
            // Wait for the bomb to explode
            while (prison.JobObject.PagedSystemMemory > lastVal)
            {
                lastVal = prison.JobObject.PagedSystemMemory;
                Assert.IsTrue(prison.JobObject.PagedSystemMemory < prisonRules.TotalPrivateMemoryLimitBytes);
                Thread.Sleep(300);
            }

            prison.Destroy();
        }
    }
}
