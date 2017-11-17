using Microsoft.VisualStudio.TestTools.UnitTesting;
using QiDiTu.StateMachineFramework.Collections;

namespace QiDiTu.StateMachineFramework.Tests
{
    [TestClass]
    public class HashSetValuedHashMapTests
    {
        [TestInitialize]
        public void Init()
        {
            setMap = new HashSetValuedHashMap<string, string>();
        }

        private IMultiValuedMap<string, string> setMap;

        [TestMethod]
        public void SetValuedMapAddTest()
        {
            Assert.IsTrue(setMap.Put("A", "a1"));
            Assert.IsTrue(setMap.Put("A", "a2"));
            Assert.IsFalse(setMap.Put("A", "a1"));
            Assert.AreEqual(2, setMap.ValuesCount());
            Assert.IsTrue(setMap.ContainsKey("A"));
        }

        [TestMethod]
        public void SetValuedMapRemoveTest()
        {
            Assert.IsTrue(setMap.Put("A", "a1"));
            Assert.IsTrue(setMap.Put("A", "a2"));
            Assert.IsFalse(setMap.Put("A", "a1"));
            Assert.AreEqual(2, setMap.ValuesCount());
            Assert.IsTrue(setMap.ContainsKey("A"));

            Assert.IsTrue(setMap.RemoveMapping("A", "a1"));
            Assert.IsTrue(setMap.RemoveMapping("A", "a2"));
            Assert.IsFalse(setMap.RemoveMapping("A", "a1"));
            Assert.AreEqual(0, setMap.ValuesCount());
            Assert.IsFalse(setMap.ContainsKey("A"));
        }

        [TestMethod]
        public void SetValuedMapRemoveViaForEachTest()
        {
            setMap.Put("A", "a1");
            setMap.Put("A", "a2");
            setMap.Put("A", "a1");

            foreach (string value in setMap.ValuesClone("A"))
            {
                setMap.RemoveMapping("A", value);
            }

            Assert.AreEqual(0, setMap.ValuesCount());
            Assert.IsFalse(setMap.ContainsKey("A"));
        }
    }
}