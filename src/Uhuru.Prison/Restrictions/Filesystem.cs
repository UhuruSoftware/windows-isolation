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

namespace Uhuru.Prison.Restrictions
{
    class Filesystem : Rule
    {
        public const string prisonRestrictionsGroup = "prisons_FilesysCell";

        public override void Apply(Prison prison)
        {
            WindowsUsersAndGroups.AddUserToGroup(prison.User.Username, prisonRestrictionsGroup);

            if (Directory.Exists(prison.Rules.PrisonHomePath))
            {
                Directory.Delete(prison.Rules.PrisonHomePath, true);
            }

            Directory.CreateDirectory(prison.Rules.PrisonHomePath);

            DirectoryInfo deploymentDirInfo = new DirectoryInfo(prison.Rules.PrisonHomePath);
            DirectorySecurity deploymentDirSecurity = deploymentDirInfo.GetAccessControl();

            // Owner is important to account for disk quota 		
            deploymentDirSecurity.SetOwner(new NTAccount(prison.User.Username));
            deploymentDirSecurity.SetAccessRule(
                new FileSystemAccessRule(
                    prison.User.Username,
                    FileSystemRights.AppendData |
                    FileSystemRights.ChangePermissions |
                    FileSystemRights.CreateDirectories |
                    FileSystemRights.CreateFiles |
                    FileSystemRights.Delete |
                    FileSystemRights.DeleteSubdirectoriesAndFiles |
                    FileSystemRights.ExecuteFile |
                    FileSystemRights.FullControl |
                    FileSystemRights.ListDirectory |
                    FileSystemRights.Modify |
                    FileSystemRights.Read |
                    FileSystemRights.ReadAndExecute |
                    FileSystemRights.ReadAttributes |
                    FileSystemRights.ReadData |
                    FileSystemRights.ReadExtendedAttributes |
                    FileSystemRights.ReadPermissions |
                    FileSystemRights.Synchronize |
                    FileSystemRights.TakeOwnership |
                    FileSystemRights.Traverse |
                    FileSystemRights.Write |
                    FileSystemRights.WriteAttributes |
                    FileSystemRights.WriteData |
                    FileSystemRights.WriteExtendedAttributes,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None | PropagationFlags.InheritOnly,
                    AccessControlType.Allow));

            // Taking ownership of a file has to be executed with0-031233332xpw0odooeoooooooooooooooooooooooooooooooooooooooooooooooooooooooooo restore privilege elevated privilages		
            using (new ProcessPrivileges.PrivilegeEnabler(Process.GetCurrentProcess(), ProcessPrivileges.Privilege.Restore))
            {
                deploymentDirInfo.SetAccessControl(deploymentDirSecurity);
            }
        }

        public override void Destroy()
        {
            throw new NotImplementedException();
        }

        public override void Init()
        {
            Filesystem.InitOpenDirectoriesList();

            if (!WindowsUsersAndGroups.ExistsGroup(prisonRestrictionsGroup))
            {
                WindowsUsersAndGroups.CreateGroup(prisonRestrictionsGroup);

                // Take ownership of c:\Windows\System32\spool\drivers\color folder
                Filesystem.TakeOwnership(Environment.UserName, @"c:\Windows\System32\spool\drivers\color");

                // Take ownership of c:\windows\tracing folder
                Filesystem.TakeOwnership(Environment.UserName, @"c:\windows\tracing");

                // Remove access to c:\Windows\tracing
                Filesystem.AddCreateSubdirDenyRule(prisonRestrictionsGroup, @"c:\windows\tracing");
                // Remove file write access to c:\Users\Public
                Filesystem.AddCreateFileDenyRule(prisonRestrictionsGroup, @"c:\windows\tracing", true);

                // Remove access to c:\ProgramData
                Filesystem.AddCreateSubdirDenyRule(prisonRestrictionsGroup, @"c:\ProgramData");
                // Remove file write access to c:\Users\Public
                Filesystem.AddCreateFileDenyRule(prisonRestrictionsGroup, @"c:\ProgramData", true);


                // Remove directory create access to c:\Users\All Users
                Filesystem.AddCreateSubdirDenyRule(prisonRestrictionsGroup, @"c:\Users\All Users", true);
                // Remove file write access to c:\Users\Public\All Users
                Filesystem.AddCreateFileDenyRule(prisonRestrictionsGroup, @"c:\Users\All Users", true);


                // Remove directory create access to c:\Users\Public
                Filesystem.AddCreateSubdirDenyRule(prisonRestrictionsGroup, @"c:\Users\Public", true);
                // Remove file write access to c:\Users\Public\Public
                Filesystem.AddCreateFileDenyRule(prisonRestrictionsGroup, @"c:\Users\Public", true);

                //// Remove directory create & file create access to profile dir
                //FilesystemCell.AddCreateSubdirDenyRule(prisonRestrictionsGroup, Path.Combine(@"c:\Users", prisonRestrictionsGroup), true);
                //FilesystemCell.AddCreateFileDenyRule(prisonRestrictionsGroup, Path.Combine(@"c:\Users", prisonRestrictionsGroup), true);

                // Remove access to other open directories
                foreach (string directory in Filesystem.OpenDirs)
                {
                    try
                    {
                        if (!directory.ToLower().StartsWith(prisonRestrictionsGroup))
                        {
                            // Remove directory create access
                            Filesystem.AddCreateSubdirDenyRule(prisonRestrictionsGroup, directory);

                            // Remove file write access
                            Filesystem.AddCreateFileDenyRule(prisonRestrictionsGroup, directory);
                        }
                    }
                    catch
                    {
                    }
                }
            }

        }

        private readonly static object openDirLock = new object();
        private static string[] openDirs = new string[0];

        public static string[] OpenDirs
        {
            get
            {
                return openDirs;
            }
        }

        public static void TakeOwnership(string user, string directory)
        {
            string command = string.Format(@"takeown /R /D Y /S localhost /U {0} /F ""{1}""", user, directory);

            int ret = Command.ExecuteCommand(command);

            if (ret != 0)
            {
                throw new Exception(@"take ownership failed.");
            }
        }

        public static void AddCreateSubdirDenyRule(string user, string directory, bool recursive = false)
        {
            string command = string.Format(@"icacls ""{0}"" /deny {1}:(AD) /c{2}", directory.Replace("\\", "/"), user, recursive ? " /t" : string.Empty);

            int ret = Command.ExecuteCommand(command);

            if (ret != 0)
            {
                throw new Exception(@"icacls command denying subdir creation failed.");
            }
        }

        public static void AddCreateFileDenyRule(string user, string directory, bool recursive = false)
        {
            string command = string.Format(@"icacls ""{0}"" /deny {1}:(W) /c{2}", directory.Replace("\\", "/"), user, recursive ? " /t" : string.Empty);
            int ret = Command.ExecuteCommand(command);

            if (ret != 0)
            {
                throw new Exception(@"icacls command denying file creation failed.");
            }
        }

        public static void InitOpenDirectoriesList()
        {
            string[] result = null;

            lock (openDirLock)
            {
                PrisonUser isolationUser = new PrisonUser("acl");
                isolationUser.Create();

                using (new UserImpersonator(isolationUser.Username, ".", isolationUser.Password, true))
                {
                    result = GetOpenDirectories(new DirectoryInfo(@"c:\")).ToArray();
                }

                isolationUser.Delete();

                openDirs = result;
            }
        }

        private static HashSet<string> GetOpenDirectories(System.IO.DirectoryInfo root)
        {
            HashSet<string> result = new HashSet<string>();
            System.IO.DirectoryInfo[] subDirs = null;

            if (root.Name.StartsWith("uhurusec_"))
            {
                result.Add(root.FullName);
                return result;
            }

            try
            {
                string adir = string.Format("uhurusec_{0}", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path.Combine(root.FullName, adir));
                result.Add(root.FullName);
                Directory.Delete(Path.Combine(root.FullName, adir));
            }
            catch
            {
            }

            try
            {
                string adir = string.Format("uhurusec_{0}", Guid.NewGuid().ToString("N"));
                File.WriteAllText(Path.Combine(root.FullName, adir + ".txt"), "test");
                result.Add(root.FullName);
                File.Delete(Path.Combine(root.FullName, adir + ".txt"));
            }
            catch
            {
            }

            try
            {
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    foreach (string subdir in GetOpenDirectories(dirInfo))
                    {
                        result.Add(subdir);
                    }
                }
            }
            catch { }

            return result;
        }

        public override RuleInstanceInfo[] List()
        {
            return new RuleInstanceInfo[0];
        }

        public override RuleType GetFlag()
        {
            return RuleType.Filesystem;
        }

        public override void Recover()
        {
            throw new NotImplementedException();
        }
    }
}
