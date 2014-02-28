namespace Uhuru.Prison.Tests.TestDirectoryTools
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Uhuru.Prison.Utilities;
    using System.IO;
    using System.Security.AccessControl;
    using System.Security.Principal;
    

    [TestClass]
    public class TestDirectoryTools
    {
        [TestMethod]
        public void GetOwnershipTest()
        {
            // Arrange
            var tmpDir = Path.Combine(Path.GetTempPath(), "dirtools-test-" + Guid.NewGuid().ToString());
            var localSystem = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

            var dirSec = new DirectorySecurity();
            dirSec.SetOwner(localSystem);

            Directory.CreateDirectory(tmpDir, dirSec);
            

            // Act
            var curIdentity = new NTAccount(Environment.UserDomainName, Environment.UserName);
            DirectoryTools.GetOwnershipForDirectory(tmpDir, curIdentity);

            // Assert
            var curDirsec = new DirectorySecurity(tmpDir, AccessControlSections.Owner);
            IdentityReference owner = curDirsec.GetOwner(typeof(NTAccount));
            Assert.IsTrue(curIdentity == owner);
        }

        [TestMethod]
        public void GetOwnershipTest2()
        {
            // Arrange
            var tmpDir = Path.Combine(Path.GetTempPath(), "dirtools-test-" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tmpDir);

            var tmpFile = Path.Combine(tmpDir, "asdf");

            var localSystem = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

            var fileSec = new FileSecurity();
            fileSec.SetOwner(localSystem);

            File.Create(tmpFile, 1, FileOptions.None, fileSec).Dispose();

            // Act
            var curIdentity = new NTAccount(Environment.UserDomainName, Environment.UserName);
            DirectoryTools.GetOwnershipForDirectory(tmpFile, curIdentity);

            // Assert
            var curFilesec = new FileSecurity(tmpFile, AccessControlSections.Owner);
            IdentityReference owner = curFilesec.GetOwner(typeof(NTAccount));
            Assert.IsTrue(curIdentity == owner);
        }
    }
}
