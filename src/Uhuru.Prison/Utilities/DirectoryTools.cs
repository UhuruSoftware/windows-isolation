using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Utilities
{
    public class DirectoryTools
    {
        static public void GetOwnershipForDirectory(string path, IdentityReference owner)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

            
            dirSecurity.SetOwner(owner);
            dirSecurity.SetAccessRule(
                new FileSystemAccessRule(
                    owner,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.InheritOnly,
                    AccessControlType.Allow
                    ));

            using (new ProcessPrivileges.PrivilegeEnabler(Process.GetCurrentProcess(), ProcessPrivileges.Privilege.Restore))
            {
                dirInfo.SetAccessControl(dirSecurity);
            }
        }

        static public void ForceDeleteDirecotry(string path)
        {
            var currentId = new NTAccount(Environment.UserDomainName, Environment.UserName);
            GetOwnershipForDirectory(path, currentId);
        }
    }
}
