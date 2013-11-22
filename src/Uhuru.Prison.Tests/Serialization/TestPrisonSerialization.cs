namespace Uhuru.Prison.Tests.Serialization
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;

    [TestClass]
    public class TestPrisonSerialization
    {
        [TestMethod]
        public void SavePrison()
        {
            // Arrange

            // Act
            Prison prison = new Prison();

            // Assert
            Assert.IsTrue(Prison.Load().Any(p => p.ID == prison.ID));
        }
    }
}