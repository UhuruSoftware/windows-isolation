using DiskQuotaTypeLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Fakes;
using System.IO;
using System.IO.Fakes;
using System.Linq;
using System.Management;
using System.Management.Fakes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Fakes;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uhuru.Prison.Allowances.Fakes;
using Uhuru.Prison.Fakes;
using Uhuru.Prison.Restrictions.Fakes;
using Uhuru.Prison.Utilities.Fakes;

namespace Uhuru.Prison.FakesUnitTest
{
    public static class PrisonTestsHelper
    {
        public static void ApplyFilesystemFakes()
        {
            ShimWindowsUsersAndGroups.AddUserToGroupStringString = (user, group) => { return; };

            ShimDirectory.ExistsString = (homePath) => { return true; };
            ShimDirectory.DeleteStringBoolean = (homePath, value) => { return; };
            ShimDirectory.CreateDirectoryString = (homePath) => { return new ShimDirectoryInfo(); };

            ShimDirectoryInfo.AllInstances.GetAccessControl = (dirSecurity) => { return new DirectorySecurity(); };
            ShimFilesystem.SetDirectoryOwnerDirectorySecurityPrison = (dirSecurity, prison) => { return; };
            ShimDirectoryInfo.AllInstances.SetAccessControlDirectorySecurity = (dirInfo, dirSecurity) => { return; };
        }

        public static void ApplyDiskRuleFakes()
        {
            ShimDisk.AllInstances.GetUserQoutaDiskQuotaManagerPrison = (disk, prison) => { return new DIDiskQuotaUser[0]; };
            ShimDisk.ShimDiskQuotaManager.SetDiskQuotaLimitStringStringInt64 = (WindowsUsername, Path, DiskQuotaBytes) => { return; };
        }

        public static void ApplyNetworkRuleFakes()
        {
            ShimManagementClass.AllInstances.CreateInstance = (@this) => { return new ShimManagementObject(@this); };
            ShimManagementObject.AllInstances.Put = (@this) => { return new ShimManagementPath(); };

            ShimManagementObjectSearcher.AllInstances.Get = (searcher) => { return new ShimManagementObjectCollection(); };
            ShimManagementObjectCollection.AllInstances.GetEnumerator = (collection) => { return new ShimManagementObjectCollection.ShimManagementObjectEnumerator(); };
        }

        public static void ApplyWindowStationRuleFakes(int winStationPtr)
        {
            ShimWindowStation.NativeOpenWindowStationString = (username) => { return new IntPtr(winStationPtr); };
            ShimWindowStation.NativeCreateWindowStationString = (username) => { return new IntPtr(winStationPtr); };
            ShimWindowStation.NativeGetProcessWindowStation = () => { return new IntPtr(winStationPtr); };
            ShimWindowStation.NativeSetProcessWindowStationIntPtr = (fakeStation) => { return true; };
            ShimWindowStation.NativeCreateDesktop = () => { return new IntPtr(winStationPtr); };
            ShimWindowStation.NativeSetProcessWindowStationIntPtr = (fakeStation) => { return true; };
        }

        public static void ApplyHttpsysFakes()
        {
            HttpsysRemovePortAccessFakes();
            HttpsysAddPortAccessFakes();
        }

        public static void ApplyIISGroupFakes()
        {
            ShimWindowsUsersAndGroups.AddUserToGroupStringString = (username, IISGroupName) => { return; };
        }

        public static void InitFilesystemRuleFakes()
        {
            ShimFilesystem.InitOpenDirectoriesList = () => { return; };

            ShimWindowsUsersAndGroups.ExistsGroupString = (group) => { return false; };
            ShimWindowsUsersAndGroups.CreateGroupString = (group) => { return; };

            ShimFilesystem.TakeOwnershipStringString = (user, directory) => { return; };
            ShimFilesystem.AddCreateSubdirDenyRuleStringStringBoolean = (group, directory, value) => { return; };
            ShimFilesystem.AddCreateFileDenyRuleStringStringBoolean = (group, directory, value) => { return; };
        }

        public static void ListNetworkRuleFakes(string username)
        {
            Dictionary<string, string> policy = new Dictionary<string, string>();
            policy.Add("Name", username);
            policy.Add("ThrottleRateAction", "819200");
            policy.Add("URIMatchCondition", null);
            policy.Add("UserMatchCondition", String.Format(@"ComputerName\{0}", username));

            List<Dictionary<string, string>> policies = new List<Dictionary<string, string>>();
            policies.Add(policy);

            ShimNetwork.GetThrottlePolicies = () => { return policies; };
        }

        public static void ListHttpsysRuleFakes()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            StreamReader sr = new StreamReader(Path.Combine(assemblyPath, @"Assets\fake_URL_reservations.txt"));
            string portAccessList = sr.ReadToEnd();

            ShimHttpsys.ListPortAccess = () => { return portAccessList; };
        }

        public static void ListWindowStationRuleFakes(IList<string> winStationMames)
        {
            ShimWindowStation.NativeEnumWindowsStationsNativeEnumWindowStationsDelegateGCHandle =
                (enumWinStationCallBack, gcHandle) =>
                {
                    foreach (var i in winStationMames)
                    {
                        enumWinStationCallBack.Invoke(i, GCHandle.ToIntPtr(gcHandle));
                    }
                    return true;
                };
        }

        public static void PrisonLoadFakes(Guid prisonId)
        {
            ShimDirectory.CreateDirectoryString = (loadLocation) => { return new ShimDirectoryInfo(); };
            ShimDirectory.GetFilesStringStringSearchOption = (path, search, option) => { return new string[1]; };

            string tempFile = CopyAssetsFilesToTemp(@"fake_prison.xml", "prison_ID", prisonId.ToString());

            //string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            FileStream fs = File.OpenRead(tempFile);
            ShimFile.OpenReadString = (prisonLocation) => { return fs; };
        }

        public static void PrisonLockdownFakes()
        {
            ShimDirectory.CreateDirectoryString = (loadLocation) => { return new ShimDirectoryInfo(); };

            // shim PrisonUser.Create
            PrisonUserCreateFakes();

            // shim Prison.CreateUserProfile
            PrisonCreateUserProfileFakes();

            // shim Prison.ChangeProfilePath
            PrisonChangeProfilePathFakes();

            // shim Prison.RunGuard
            PrisonRunGuardFakes();

            // shim Prison.Save
            PrisonSaveFakes();
        }

        public static void PrisonDestroyFakes()
        {
            PrisonUserDeleteFakes();

            ShimPrison.AllInstances.SystemRemoveQuota = (identifier) => { return; };

            DeletePersistedPrisonFakes();
        }

        public static void HttpsysRemovePortAccessFakes()
        {
            ShimCommand.ExecuteCommandString = (command) => { return 1; };
        }

        public static void PrisonCreateProcessAsUserFakes(Native.PROCESS_INFORMATION processInfo)
        {
            //shim Prison.GetDefaultEnvironmentVarialbes
            ShimPrison.AllInstances.GetDefaultEnvironmentVarialbes = (pris) => { return new Dictionary<string, string>(); };

            ShimPrison.AllInstances.NativeCreateProcessAsUserBooleanStringStringStringStringPipeStreamPipeStreamPipeStream = (pris, interactive, filename, arguments, cd, envBlock, x, y, z) =>
            {
                return processInfo;
            };
        }

        public static void PersistanceReadDataFake(string username)
        {
            ShimFile.ExistsString = (location) => { return true; };

            string tempFile = CopyAssetsFilesToTemp(@"fake_prisondb.xml", "prison_user_name", username);

            FileStream fs = File.OpenRead(tempFile);
            ShimFile.OpenReadString = (prisonLocation) => { return fs; };
        }

        private static void PrisonUserCreateFakes()
        {
            ShimWindowsUsersAndGroups.CreateUserStringString = (username, password) => { return; };
            ShimWindowsUsersAndGroups.GetLocalUserSidString = (username) => { return "a string"; };
            ShimPersistence.SaveValueStringStringObject = (group, key, value) => { return; };
        }

        private static void PrisonCreateUserProfileFakes()
        {
            ShimPrison.AllInstances.InitializeLogonToken = (pris) => { return; };
            ShimPrison.AllInstances.LoadUserProfile = (pris) => { return; };
            ShimPrison.AllInstances.UnloadUserProfile = (pris) => { return; };
        }

        private static void PrisonChangeProfilePathFakes()
        {
            ShimPrison.AllInstances.UnloadUserProfileUntilReleased = (pris) => { return; };
            ShimPrison.AllInstances.GetNativeUserProfileDirectoryStringBuilder = (pris, a) => { return; };
            ShimPrison.AllInstances.ChangeRegistryUserProfileString = (pris, destination) => { return; };

            ShimDirectory.MoveStringString = (initial, destination) => { return; };
        }

        private static void PrisonRunGuardFakes()
        {
            ShimProcess.StartProcessStartInfo = (processStartInfo) => { return new Process(); };
            ShimPrison.AllInstances.CheckGuard = (fakePrison) => { return; };
        }

        private static void PrisonSaveFakes()
        {
            System.IO.Fakes.ShimFileStream.ConstructorStringFileMode =
                    (@this, p, f) =>
                    {
                        var shim = new System.IO.Fakes.ShimFileStream(@this);
                    };
            ShimDataContractSerializer.AllInstances.WriteObjectContentXmlWriterObject = (data, writeStream, fakePrison) =>
            { return; };
        }

        private static void PrisonUserDeleteFakes()
        {
            ShimWindowsUsersAndGroups.ExistsUserString = (username) => { return true; };
            ShimWindowsUsersAndGroups.DeleteUserString = (username) => { return; };
        }

        private static void DeletePersistedPrisonFakes()
        {
            ShimFile.DeleteString = (prisonFile) => { return; };
        }

        private static void HttpsysAddPortAccessFakes()
        {
            ShimCommand.ExecuteCommandString = (command) => { return 0; };
        }

        private static string CopyAssetsFilesToTemp(string file, string valueToReplace, string value)
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var content = string.Empty;

            using (StreamReader reader = new StreamReader(Path.Combine(assemblyPath, String.Format(@"Assets\{0}", file))))
            {
                content = reader.ReadToEnd();
                reader.Close();
            }

            content = content.Replace(valueToReplace, value);
            string tempFile = Path.Combine(Path.GetTempPath(), file);

            using (StreamWriter writer = new StreamWriter(tempFile))
            {
                writer.Write(content);
                writer.Flush();
                writer.Close();
            }

            return tempFile;
        }
    }
}
