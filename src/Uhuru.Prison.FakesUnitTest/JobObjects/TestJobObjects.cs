using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using Uhuru.Prison.Fakes;
using System.DirectoryServices.AccountManagement.Fakes;
using Uhuru.Prison.Utilities.Fakes;
using System.Security.Principal.Fakes;
using System.IO.Fakes;
using System.Diagnostics;
using Uhuru.Prison.Utilities.WindowsJobObjects.Fakes;
using System.Diagnostics.Fakes;
using System.Runtime.Serialization.Fakes;
using System.Security.AccessControl.Fakes;
using System.Security.AccessControl;
using Uhuru.Prison.Restrictions.Fakes;
using DiskQuotaTypeLibrary;
using System.Management.Fakes;

namespace Uhuru.Prison.FakesUnitTest.JobObjects
{
    /// <summary>
    /// Summary description for TestJobObjects
    /// </summary>
    [TestClass]
    public class TestJobObjects
    {
        [TestMethod]
        public void TestSimpleEcho()
        {
            using (ShimsContext.Create())
            {
                // shim Prison.Lockdown
                PrisonTestsHelper.PrisonLockdownFakes();

                Prison prison = new Prison();
                prison.Tag = "uhtst";

                PrisonRules prisonRules = new PrisonRules();
                prisonRules.CellType = RuleType.None;
                prisonRules.PrisonHomePath = @"c:\prison_tests\p3";

                prison.Lockdown(prisonRules);

                
                // shim Prison.Execute
                Native.PROCESS_INFORMATION processInfo = new Native.PROCESS_INFORMATION
                {
                    hProcess = new IntPtr(2400),
                    hThread = new IntPtr(2416),
                    dwProcessId = 5400,
                    dwThreadId = 4544
                };

                PrisonTestsHelper.PrisonCreateProcessAsUserFakes(processInfo);
                var shimedProcess = new ShimProcess();
                shimedProcess.IdGet = () => { return processInfo.dwProcessId; };
                var raisingEventsChangedTo = false;
                shimedProcess.EnableRaisingEventsSetBoolean = (x) => { raisingEventsChangedTo = x; };
                ShimProcess.GetProcessByIdInt32 = (id) => { return (Process)shimedProcess; };

                Process procAddedToJob = null;
                ShimJobObject.AllInstances.AddProcessProcess = (jobObject, proc) => { procAddedToJob = proc; return; };
                ShimPrison.AllInstances.AddProcessToGuardJobObjectProcess = (fakePrison, proc) => { return; };
                var threadIdResumed = 0;
                ShimPrison.AllInstances.NativeResumeThreadInt32 = (fakePrison, threadId) => { threadIdResumed = threadId; return 1; };

                // Act
                Process process = prison.Execute(
                    @"c:\windows\system32\cmd.exe",
                    @"/c echo test");

                // Assert
                Assert.AreEqual(processInfo.dwProcessId, process.Id);
                Assert.AreEqual(processInfo.dwThreadId, threadIdResumed);
                Assert.AreEqual(procAddedToJob.Id, process.Id);
                Assert.AreEqual(true, raisingEventsChangedTo);
            }
        }

    }
}
