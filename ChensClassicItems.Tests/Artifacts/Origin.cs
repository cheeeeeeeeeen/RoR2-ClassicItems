using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClassBeingTested = Chen.ClassicItems.Origin;

namespace Chen.ClassicItems.Tests.Artifacts
{
    [TestClass]
    public class Origin
    {
        [TestMethod]
        public void DebugCheck_Toggled_ReturnsFalse()
        {
            bool result = ClassBeingTested.DebugCheck();

            Assert.IsFalse(result);
        }
    }
}