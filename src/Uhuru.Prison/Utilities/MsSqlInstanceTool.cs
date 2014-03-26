// -----------------------------------------------------------------------
// <copyright file="FirewallTools.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------


namespace Uhuru.Prison
{
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

    public static class MsSqlInstanceTool
    {
        /// <summary>
        /// Give access to read the default SQL Server instance files and write to the registry at HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\
        /// </summary>
        /// <param name="prison">prison object associated to the gear</param>
        /// <param name="instanceType">MsSql server instance type MSSQL10_50/MSSQL11</param>
        /// <param name="defaultInstanceName">MsSql default instance MSSQLSERVER/MSSQLSERVER2012</param>
        public static void ConfigureMsSqlInstanceRegistry(Prison prison, string instanceType, string defaultInstanceName)
        {
            try
            {
                var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                string sqlPath = hklm.OpenSubKey(string.Format(@"SOFTWARE\Microsoft\Microsoft SQL Server\{0}.{1}\Setup", instanceType, defaultInstanceName), true).GetValue("SQLPath", string.Empty).ToString();

                if (!string.IsNullOrWhiteSpace(sqlPath) && Directory.Exists(sqlPath))
                {
                    AllowReadOfBaseMSSQLInstance(sqlPath, prison);

                    string instanceName = string.Format("Instance{0}", prison.Rules.UrlPortAccess);

                    hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL", true).SetValue(instanceName, string.Format("{0}.{1}", instanceType, instanceName), RegistryValueKind.String);

                    string instanceRegistryKey1 = string.Format(@"SOFTWARE\Microsoft\Microsoft SQL Server\{0}", instanceName);
                    hklm.CreateSubKey(instanceRegistryKey1);

                    string instanceRegistryKey2 = string.Format(@"SOFTWARE\Microsoft\Microsoft SQL Server\{0}.{1}", instanceType, instanceName);
                    hklm.CreateSubKey(instanceRegistryKey2);

                    string instanceRegistryKey3 = string.Format(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft SQL Server\{0}", instanceName);
                    hklm.CreateSubKey(instanceRegistryKey3);


                    // Give registry access
                    GrantRegistryAccess(instanceRegistryKey1, prison);
                    GrantRegistryAccess(instanceRegistryKey2, prison);
                    GrantRegistryAccess(instanceRegistryKey3, prison);
                    GrantRegistryAccess(@"SYSTEM\CurrentControlSet\Services\WinSock2\Parameters", prison);
                }

            }
            catch (Exception ex)
            {
                Logger.Error("There was an error while applying MsSqlInstance Prison Rule: {0} - {1}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        // TODO (adas): when prison user gets destroyed should remove the MsSql instance from registry too
        public static void RemoveUnusedMsSqlInstanceFromRegistry()
        { }

        private static void AllowReadOfBaseMSSQLInstance(string path, Prison prison)
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

        private static void GrantRegistryAccess(string key, Prison prison)
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
    }
}
