using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uhuru.Prison.Data.Tests
{
    [TestClass]
    public class TestPrison
    {
        [TestMethod]
        public void TestCreatePrison()
        {
            // Arrange
            using (PrisonEntities entities = Connection.GetPrisonEntities())
            {
                Prison newPrison = new Prison();
                newPrison.Description = "An entity created for testsing.";

                // Act
                entities.Prisons.Add(newPrison);
                entities.SaveChanges();

                // Assert
                Assert.AreEqual(entities.Prisons.Count(), 1);
            }
        }
    }
}
