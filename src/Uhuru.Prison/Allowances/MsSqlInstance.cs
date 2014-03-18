using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Prison.Utilities;

namespace Uhuru.Prison.Allowances
{
    // Give access to read the default SQL Server instance files and write to the registry at HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\
    class MsSqlInstance : Rule
    {
        const string MSSQLGroupName = "SQLServerMSSQLUser${0}$MSSQLSERVER";

        public override void Apply(Prison prison)
        {
            try
            {
                var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                string sqlPath = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\Setup", true).GetValue("SQLPath", string.Empty).ToString();

                if (!string.IsNullOrWhiteSpace(sqlPath) && Directory.Exists(sqlPath))
                {
                    AllowReadOfBaseMSSQLInstance(sqlPath, prison);

                    string instanceName = string.Format("Instance{0}", prison.Rules.UrlPortAccess);

                    hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL", true).SetValue(instanceName, string.Format("MSSQL10_50.{0}", instanceName), RegistryValueKind.String);

                    string instanceRegistryKey1 = string.Format(@"SOFTWARE\Microsoft\Microsoft SQL Server\{0}", instanceName);
                    hklm.CreateSubKey(instanceRegistryKey1);

                    string instanceRegistryKey2 = string.Format(@"SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.{0}", instanceName);
                    hklm.CreateSubKey(instanceRegistryKey2);

                    string instanceRegistryKey3 = string.Format(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft SQL Server\{0}", instanceName);
                    hklm.CreateSubKey(instanceRegistryKey3);

                    // Give registry access
                    this.GrantRegistryAccess(instanceRegistryKey1, prison);
                    this.GrantRegistryAccess(instanceRegistryKey2, prison);
                    this.GrantRegistryAccess(instanceRegistryKey3, prison);
                    this.GrantRegistryAccess(@"SYSTEM\CurrentControlSet\Services\WinSock2\Parameters", prison);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("There was an error while applying MsSqlInstance Prison Rule: {0} - {1}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        private void AllowReadOfBaseMSSQLInstance(string path, Prison prison)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

            dirSecurity.SetAccessRule(
                new FileSystemAccessRule(
                    prison.User.Username,
                    FileSystemRights.Read | FileSystemRights.ListDirectory | FileSystemRights.ReadAttributes | FileSystemRights.ReadData | FileSystemRights.Synchronize,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

            using (new ProcessPrivileges.PrivilegeEnabler(Process.GetCurrentProcess(), ProcessPrivileges.Privilege.Restore))
            {
                dirInfo.SetAccessControl(dirSecurity);
            }
        }

        private void GrantRegistryAccess(string key, Prison prison)
        {
            NTAccount account = new NTAccount(null, prison.User.Username);
            var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

            using (RegistryKey rk = hklm.OpenSubKey(key, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl))
            {
                RegistrySecurity rs = rk.GetAccessControl();
                RegistryAccessRule rar = new RegistryAccessRule(
                    account.ToString(),
                    RegistryRights.FullControl,
                    InheritanceFlags.ContainerInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                rs.AddAccessRule(rar);
                rk.SetAccessControl(rs);
            }
        }

        public override void Destroy(Prison prison)
        {
        }

        public override RuleInstanceInfo[] List()
        {
            throw new NotImplementedException();           
        }

        public override void Init()
        {
        }

        public override RuleType GetFlag()
        {
            return RuleType.MsSqlInstance;
        }

        public override void Recover(Prison prison)
        {
        }
    }
}
