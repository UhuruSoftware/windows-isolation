using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Tests
{
    [TestClass]
    public class TestPersistence
    {
        [TestMethod]
        public void WriteReadAndDeleteData()
        {
            // Arrange
            Persistence.SaveValue("test", "mykey", "myvalue");

            // Act
            string value = (string)Persistence.ReadValue("test", "mykey");
            Persistence.SaveValue("test", "mykey", null);

            // Assert
            Assert.AreEqual("myvalue", value);
            Assert.AreEqual(null, Persistence.ReadValue("test", "mykey"));
        }
    }
}
