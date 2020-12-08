using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chen.ClassicItems.Tests
{
    [TestClass]
    public class ClassicItems
    {
        [TestMethod]
        public void ModVer_Length_ReturnsCorrectFormat()
        {
            string result = ClassicItemsPlugin.ModVer;
            const int ModVersionCount = 3;

            int count = result.Split('.').Length;

            Assert.AreEqual(ModVersionCount, count);
        }

        [TestMethod]
        public void ModName_Value_ReturnsCorrectName()
        {
            string result = ClassicItemsPlugin.ModName;
            const string ModName = "ChensClassicItems";

            Assert.AreEqual(ModName, result);
        }

        [TestMethod]
        public void ModGuid_Value_ReturnsCorrectGuid()
        {
            string result = ClassicItemsPlugin.ModGuid;
            const string ModGuid = "com.Chen.ChensClassicItems";

            Assert.AreEqual(ModGuid, result);
        }
    }
}