using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Uhuru.Prison.Tests.Rules
{
    [TestClass]
    public class TestUsers
    {
        [TestMethod]
        public void CredentialGenerationWithPrefix()
        {
            // Arrange
            PrisonUser user = new PrisonUser("untst");

            // Act
            string username = user.Username;
            string password = user.Password;
            string prefix = user.UsernamePrefix;
            List<string> usernamePieces = username.Split(new string[] {"_"}, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Assert
            Assert.AreEqual(3, usernamePieces.Count);
            Assert.AreEqual("untst", prefix);
            Assert.IsFalse(string.IsNullOrWhiteSpace(username));
            Assert.IsFalse(string.IsNullOrWhiteSpace(password));
            Assert.IsTrue(username.Contains("untst"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "This user has not been created yet.")]
        public void CannotDeleteNotCreatedUser()
        {
            // Arrange
            PrisonUser user = new PrisonUser("untst");

            // Act
            user.Delete();

            // Assert
            // Expected exception;
        }

        [TestMethod]
        public void CreateAndDeleteUser()
        {
            // Arrange
            PrisonUser user = new PrisonUser("untst");

            // Act
            user.Create();

            // Assert
            Assert.IsTrue(PrisonUser.ListUsers("untst").Any(u => u.Username == user.Username));

            // Act
            user.Delete();

            // Assert
            Assert.IsFalse(PrisonUser.ListUsers("untst").Any(u => u.Username == user.Username));
        }
    }
}
