namespace Uhuru.Prison.Utilities
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;
    using System.Threading.Tasks;

    public static class SystemVirtualAddressSpaceQuotas
    {
        // As documented in Windows Internals book. There is an error there. It misses the Control subkey :( 
        // static private string quotaKey                          = @"SYSTEM\CurrentControlSet\Session Manager\Quota System";
        static private string quotaKey                          = @"SYSTEM\CurrentControlSet\Control\Session Manager\Quota System";
        

        static private string pagedPoolQuotaValueName           = "PagedPoolQuota";
        static private string nonPagedPoolQuotaValueName        = "NonPagedPoolQuota";
        static private string pagingFileQuotaValueName          = "PagingFileQuota";
        static private string workingSetPagesQuotaValueName     = "WorkingSetPagesQuota";

        public static void SetPagedPoolQuota(long sizeBytes, IdentityReference user)
        {
            string sid = user.Translate(typeof(SecurityIdentifier)).Value;

            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (var quotaKey = hklm.CreateSubKey(SystemVirtualAddressSpaceQuotas.quotaKey))
                {
                    using (var usersQuotaKey = quotaKey.CreateSubKey(sid))
                    {
                        usersQuotaKey.SetValue(pagedPoolQuotaValueName, sizeBytes / 1024 / 1024, RegistryValueKind.DWord);
                    }
                }
            }
        }

        public static void SetNonPagedPoolQuota(long sizeBytes, IdentityReference user)
        {
            string sid = user.Translate(typeof(SecurityIdentifier)).Value;

            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (var quotaKey = hklm.CreateSubKey(SystemVirtualAddressSpaceQuotas.quotaKey))
                {
                    using (var usersQuotaKey = quotaKey.CreateSubKey(sid))
                    {
                        usersQuotaKey.SetValue(nonPagedPoolQuotaValueName, sizeBytes / 1024 / 1024, RegistryValueKind.DWord);
                    }
                }
            }
        }

        public static void SetPagingFileQuota(long sizeBytes, IdentityReference user)
        {
            string sid = user.Translate(typeof(SecurityIdentifier)).Value;

            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (var quotaKey = hklm.CreateSubKey(SystemVirtualAddressSpaceQuotas.quotaKey))
                {
                    using (var usersQuotaKey = quotaKey.CreateSubKey(sid))
                    {
                        usersQuotaKey.SetValue(pagingFileQuotaValueName, sizeBytes / Environment.SystemPageSize, RegistryValueKind.DWord);
                    }
                }
            }
        }

        public static void SetWorkingSetPagesQuota(long sizeBytes, IdentityReference user)
        {
            string sid = user.Translate(typeof(SecurityIdentifier)).Value;

            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (var quotaKey = hklm.CreateSubKey(SystemVirtualAddressSpaceQuotas.quotaKey))
                {
                    using (var usersQuotaKey = quotaKey.CreateSubKey(sid))
                    {
                        // this will block `testlimit -v`
                        usersQuotaKey.SetValue(workingSetPagesQuotaValueName, sizeBytes / Environment.SystemPageSize, RegistryValueKind.DWord);
                        
                    }
                }
            }
        }

        public static void RemoveQuotas(IdentityReference user)
        {
            string sid = user.Translate(typeof(SecurityIdentifier)).Value;

            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (var quotaKey = hklm.CreateSubKey(SystemVirtualAddressSpaceQuotas.quotaKey))
                {
                    quotaKey.DeleteSubKey(sid, false);
                }
            }
        }
    }
}
