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
    public class TestFilesystemRule
    {
        [TestMethod]
        public void AllowAccessInHomeDir()
        {
            // Arrange
            Prison.Init();
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.Filesystem;
            prisonRules.PrisonHomePath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
@"
File.WriteAllText(Guid.NewGuid().ToString(""N""), Guid.NewGuid().ToString());
", prison);
            
            Process process = prison.Execute(exe);

            process.WaitForExit();

            // Assert
            Assert.AreEqual(0, process.ExitCode);
        }

        [TestMethod]
        public void DisallowAccessEverywhereElse()
        {
            // Arrange
            Prison.Init();
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.Filesystem;
            prisonRules.PrisonHomePath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
@"
  return WalkDirectoryTree(new DirectoryInfo(@""c:\""));
}

static int WalkDirectoryTree(System.IO.DirectoryInfo root)
{
    System.IO.DirectoryInfo[] subDirs = null;

    // First, process all the files directly under this folder 
    try
    {
        string adir = Guid.NewGuid().ToString(""N"");
        Directory.CreateDirectory(Path.Combine(root.FullName, adir));
        Directory.Delete(Path.Combine(root.FullName, adir));
        return 1;
    }
    catch { }

    try
    {
        string adir = Guid.NewGuid().ToString(""N"");
        File.WriteAllText(Path.Combine(root.FullName, adir), ""test"");
        File.Delete(Path.Combine(root.FullName, adir));
        return 1;
    }
    catch { }

    try
    {
        subDirs = root.GetDirectories();

        foreach (System.IO.DirectoryInfo dirInfo in subDirs)
        {
            // Resursive call for each subdirectory.
            return WalkDirectoryTree(dirInfo);
        }
    }
    catch { }
    return 0;
}

static int Dummy()
{
", prison);

            Process process = prison.Execute(exe);

            process.WaitForExit();

            // Assert
            Assert.AreEqual(0, process.ExitCode);
        }


    }
}
