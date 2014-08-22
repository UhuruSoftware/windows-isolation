using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using Uhuru.Prison.Utilities.Fakes;
using Uhuru.Prison.Fakes;
using System.Runtime.Serialization.Fakes;
using System.Collections.Generic;
using System.Diagnostics.Fakes;
using System.Diagnostics;
using Uhuru.Prison.Utilities.WindowsJobObjects.Fakes;
using System.IO.Pipes;
using System.IO.Pipes.Fakes;

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
                ShimXmlObjectSerializer.AllInstances.WriteObjectStreamObject = (data, writeStream, fakePrison) =>
                {
                    saveWasInvoked = true;
                    return;
                };

                //ShimPrison.AllInstances.TestFuncInt32Int32OutInt32Ref = (Prison p, int a, out int b, ref int c) => { b = 4; };

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

        [TestMethod]
        public void TestExecuteStdStreams()
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

                Uhuru.Prison.Native.STARTUPINFO savedStartInfo = new Native.STARTUPINFO();

                ShimPrison.AllInstances.GetDefaultEnvironmentVarialbes = (pris) => { return new Dictionary<string, string>(); };
                ShimPrison.NativeCreateProcessAsUserSafeTokenHandleStringStringNativeSECURITY_ATTRIBUTESRefNativeSECURITY_ATTRIBUTESRefBooleanNativeProcessCreationFlagsStringStringNativeSTARTUPINFORefNativePROCESS_INFORMATIONOut = 
                    (       SafeTokenHandle hToken,
                            string lpApplicationName,
                            string lpCommandLine,
                            ref Uhuru.Prison.Native.SECURITY_ATTRIBUTES lpProcessAttributes,
                            ref Uhuru.Prison.Native.SECURITY_ATTRIBUTES lpThreadAttributes,
                            bool bInheritHandles,
                            Uhuru.Prison.Native.ProcessCreationFlags dwCreationFlags,
                            string lpEnvironment,
                            string lpCurrentDirectory,
                            ref Uhuru.Prison.Native.STARTUPINFO lpStartupInfo,
                            out Uhuru.Prison.Native.PROCESS_INFORMATION lpProcessInformation) => 
                        {
                            savedStartInfo = lpStartupInfo;
                            lpProcessInformation = processInfo; return true; 
                        };
                

                ShimPrison.GetCurrentSessionId = () => { return 0; };

                var shimedProcess = new ShimProcess();
                shimedProcess.IdGet = () => { return processInfo.dwProcessId; };
                shimedProcess.EnableRaisingEventsSetBoolean = (value) => { };
                
                ShimProcess.GetProcessByIdInt32 = (id) => { return (Process)shimedProcess; };

                var pp = new NamedPipeServerStream("asdfasdf");

                var shimedStdin = new ShimNamedPipeServerStream();
                var shimedStdout = new ShimNamedPipeServerStream();
                var shimedStderr = new ShimNamedPipeServerStream();


                ShimPrison.GetHandleFromPipePipeStream = (ps) => {
                    if (ps == shimedStdin.Instance)
                    {
                        return new IntPtr(1);
                    }
                    if (ps == shimedStdout.Instance)
                    {
                        return new IntPtr(2);
                    }
                    if (ps == shimedStderr.Instance)
                    {
                        return new IntPtr(3);
                    }
                    return IntPtr.Zero;
                };


                Process procAddedToJob = null;
                ShimJobObject.AllInstances.AddProcessProcess = (jobObject, proc) => { procAddedToJob = proc; return; };
                ShimPrison.AllInstances.AddProcessToGuardJobObjectProcess = (fakePrison, proc) => { return; };
                
                ShimPrison.AllInstances.ResumeProcessProcess = (fakePrison, pProcess) => { };

                // Act
                Process process = prison.Execute(
                    @"c:\windows\system32\cmd.exe",
                    @"/c echo test", "", false, null, shimedStdin.Instance, shimedStdout.Instance, shimedStderr.Instance);


                // Assert
                Assert.AreEqual(savedStartInfo.hStdInput, new IntPtr(1));
                Assert.AreEqual(savedStartInfo.hStdOutput, new IntPtr(2));
                Assert.AreEqual(savedStartInfo.hStdError, new IntPtr(3));
                
            }
        }
    }
}
