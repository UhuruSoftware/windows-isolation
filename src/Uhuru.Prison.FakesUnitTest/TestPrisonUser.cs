using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.QualityTools.Testing.Fakes;
using Uhuru.Prison.Fakes;
using Uhuru.Prison.Utilities.Fakes;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Uhuru.Prison.FakesUnitTest
{
    [TestClass]
    public class TestPrisonUser
    {
        //private string Username = "prison_uhtst_buVodrR";

        //[TestInitialize]
        //public void InitializePrisondbFile()
        //{
        //    string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    string filePath = Path.Combine(assemblyPath, @"Assets\fake_prisondb.xml");
        //    var content = string.Empty;
        //    using (StreamReader reader = new StreamReader(filePath))
        //    {
        //        content = reader.ReadToEnd();
        //        reader.Close();
        //    }

        //    content = content.Replace("prison_user_name", Username);
        //    string tempFile = Path.Combine(Path.GetTempPath(), @"fake_prisondb.xml");

        //    using (StreamWriter writer = new StreamWriter(tempFile))
        //    {
        //        writer.Write(content);
        //        writer.Flush();
        //        writer.Close();
        //    }

        //    PrisonTestsHelper.PersistanceReadDataFake(Username);
        //}

        [TestMethod]
        public void TestCreatePrisonUserOK()
        {           
            using (ShimsContext.Create())
            {
                string newuser = string.Empty;

                ShimWindowsUsersAndGroups.ExistsUserString = (username) => { return false; };
                ShimWindowsUsersAndGroups.CreateUserStringString = (username, password) => { newuser = username; return; };
                ShimWindowsUsersAndGroups.GetLocalUserSidString = (username) => { return "a string"; };

                ShimPersistence.SaveValueStringStringObject = (group, key, value) => { return; };

                PrisonUser user = new PrisonUser("untst");
                user.Create();

                ShimWindowsUsersAndGroups.GetUsers = () => { return new string [] { "Administrator", "Guest", "openshift_service", newuser }; };
                PrisonTestsHelper.PersistanceReadDataFake(newuser);
                PrisonUser[] users = PrisonUser.ListUsers("untst");

                Assert.IsTrue(Array.Find(users, u => u.Username == user.Username) != null);              
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "This windows user already exists.")]
        public void TestCreatePrisonUser_UserExist()
        {
            using (ShimsContext.Create())
            {
                ShimWindowsUsersAndGroups.ExistsUserString = (username) => { return true; };

                PrisonUser user = new PrisonUser("untst");
                user.Create();
            }
        }

        [TestMethod]
        public void TestDeletePrisonUserOK()
        {
            using (ShimsContext.Create())
            {
                ShimWindowsUsersAndGroups.ExistsUserString = (username) => { return false; };
                ShimWindowsUsersAndGroups.CreateUserStringString = (username, password) => { return; };
                ShimWindowsUsersAndGroups.GetLocalUserSidString = (username) => { return "a string"; };

                PrisonUser user = new PrisonUser("untst");
                user.Create();

                ShimWindowsUsersAndGroups.ExistsUserString = (username) => { return true; };
                ShimWindowsUsersAndGroups.DeleteUserString = (username) => { return; };
                user.Delete();

                ShimWindowsUsersAndGroups.GetUsers = () => { return new string[] { "Administrator", "Guest", "openshift_service"}; };
                PrisonUser[] users = PrisonUser.ListUsers("untst");

                Assert.IsTrue(Array.Find(users, u => u.Username == user.Username) == null);

            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "This user has not been created yet.")]
        public void TestDeletePrisonUser_UserNotCreated()
        {
            using (ShimsContext.Create())
            {
                ShimWindowsUsersAndGroups.ExistsUserString = (username) => { return false; };

                PrisonUser user = new PrisonUser("untst");
                user.Delete();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "Cannot find this windows user.")]
        public void TestDeletePrisonUser_UserDoNotExist()
        {
            using (ShimsContext.Create())
            {
                ShimWindowsUsersAndGroups.ExistsUserString = (username) => { return false; };
                ShimWindowsUsersAndGroups.CreateUserStringString = (username, password) => { return; };
                ShimWindowsUsersAndGroups.GetLocalUserSidString = (username) => { return "a string"; };

                PrisonUser user = new PrisonUser("untst");
                user.Create();

                ShimWindowsUsersAndGroups.ExistsUserString = (username) => { return false; };
                user.Delete();
            }
        }

        [TestMethod]
        public void TestListPrisonUsers()
        {
            string username = "prison_uhtst_buVodrR";
            string[] newwinusers = { "Administrator", "Guest", "openshift_service", username };

            using (ShimsContext.Create())
            {
                ShimWindowsUsersAndGroups.GetUsers = () => { return newwinusers; };
                PrisonTestsHelper.PersistanceReadDataFake(username);
                PrisonUser[] users = PrisonUser.ListUsers();

                Assert.IsTrue(users.Length == Array.FindAll(newwinusers, user => user.StartsWith("prison")).Length);
            }
        }

    }
}
