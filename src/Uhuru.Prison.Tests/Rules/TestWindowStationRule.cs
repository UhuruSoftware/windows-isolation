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
    public class TestWindowStationRule
    {

        [TestMethod]
        public void AssignNewDesktop()
        {
            // Arrange
            Prison prison = new Prison();
            prison.Tag = "uhtst";

            PrisonRules prisonRules = new PrisonRules();
            prisonRules.CellType = RuleType.WindowStation;
            prisonRules.PrisonHomePath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

       

            // Act
            string exe = Utilities.CreateExeForPrison(
string.Format(@"

byte[] name = new byte[1024];
uint actualLength;
GetUserObjectInformation(GetProcessWindowStation(), UOI_NAME, name, 1024, out actualLength);

string workstationName = ASCIIEncoding.ASCII.GetString(name, 0, (int)actualLength - 1);

if (workstationName != ""{0}"")
{{
return 1;
}}

return 0;   

}}

[DllImport(""user32.dll"", SetLastError = true)]
public static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex,
    [Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded);

[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
[DllImport(""user32"", CharSet = CharSet.Unicode, SetLastError = true)]
internal static extern IntPtr GetProcessWindowStation();

public const int UOI_FLAGS = 1;
public const int UOI_NAME = 2;
public const int UOI_TYPE = 3;
public const int UOI_USER_SID = 4;
public const int UOI_HEAPSIZE = 5; //Windows Server 2003 and Windows XP/2000:  This value is not supported.
public const int UOI_IO = 6;

private static int Dummy()
{{
", prison.User.Username), prison);

            Process process = prison.Execute(exe, "", true);

            process.WaitForExit();

            // Assert
            Assert.AreEqual(0, process.ExitCode);
        }
    }
}
