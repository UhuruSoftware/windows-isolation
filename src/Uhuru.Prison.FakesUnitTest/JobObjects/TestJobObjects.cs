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
using Uhuru.Prison.ExecutorService.Fakes;
using System.ServiceModel.Fakes;
using Uhuru.Prison.ExecutorService;
using System.ServiceModel;

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

                ShimPrison.GetCurrentSessionId = () => { return 0; };

                var shimedProcess = new ShimProcess();
                shimedProcess.IdGet = () => { return processInfo.dwProcessId; };
                var raisingEventsChangedTo = false;
                shimedProcess.EnableRaisingEventsSetBoolean = (value) => { raisingEventsChangedTo = value; };
                ShimProcess.GetProcessByIdInt32 = (id) => { return (Process)shimedProcess; };

                Process procAddedToJob = null;
                ShimJobObject.AllInstances.AddProcessProcess = (jobObject, proc) => { procAddedToJob = proc; return; };
                ShimPrison.AllInstances.AddProcessToGuardJobObjectProcess = (fakePrison, proc) => { return; };
                var processIdResumed = 0;
                ShimPrison.AllInstances.ResumeProcessProcess = (fakePrison, pProcess) => { processIdResumed = pProcess.Id; };

                // Act
                Process process = prison.Execute(
                    @"c:\windows\system32\cmd.exe",
                    @"/c echo test");

                // Assert
                Assert.AreEqual(processInfo.dwProcessId, process.Id);
                Assert.AreEqual(processInfo.dwProcessId, processIdResumed);
                Assert.AreEqual(procAddedToJob.Id, process.Id);
                Assert.AreEqual(true, raisingEventsChangedTo);
            }
        }

        [TestMethod]
        public void TestSimpleEchoChangedSession()
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

                ShimPrison.GetCurrentSessionId = () => { return 12; };

                ShimPrison.InitChangeSessionServiceString = (tempSeriviceId) => { return; };

                StubIExecutor exec = new StubIExecutor();
                ShimChannelFactory<IExecutor>.AllInstances.CreateChannel = (executor) => { return exec; };
                exec.ExecuteProcessPrisonStringStringStringDictionaryOfStringStringPipeStreamPipeStreamPipeStream =
                    (fakePrison, filename, arguments, cd, extraEnvironmentVariables, stdinPipeName, stdoutPipeName, stderrPipeName) =>
                    {
                        return processInfo.dwProcessId;
                    };

                var shimedProcess = new ShimProcess();
                shimedProcess.IdGet = () => { return processInfo.dwProcessId; };
                var raisingEventsChangedTo = false;
                shimedProcess.EnableRaisingEventsSetBoolean = (value) => { raisingEventsChangedTo = value; };
                ShimProcess.GetProcessByIdInt32 = (id) => { return (Process)shimedProcess; };

                ShimPrison.AllInstances.CloseRemoteSessionIExecutor = (fakePrison, executor) => { return; };
               
                var processIdResumed = 0;
                ShimPrison.AllInstances.ResumeProcessProcess = (fakePrison, pProcess) => { processIdResumed = pProcess.Id; };

                ShimPrison.RemoveChangeSessionServiceString = (sessionId) => { return; };
                // Act
                Process process = prison.Execute(
                    @"c:\windows\system32\cmd.exe",
                    @"/c echo test");

                // Assert
                Assert.AreEqual(processInfo.dwProcessId, process.Id);
                Assert.AreEqual(processInfo.dwProcessId, processIdResumed);
                Assert.AreEqual(true, raisingEventsChangedTo);
            }
        }
    }
}
