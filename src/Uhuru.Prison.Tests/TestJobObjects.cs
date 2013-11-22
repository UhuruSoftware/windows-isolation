using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Tests
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
        [ExpectedException(typeof(InvalidOperationException), "This prison has already been used to execute something.")]
        public void TestMultipleEcho()
        {
            // Arrange
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.None;

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
        }
    }
}
