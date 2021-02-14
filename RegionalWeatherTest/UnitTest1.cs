using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RegionalWeatherTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            true.Should().BeTrue();
        }
    }
}