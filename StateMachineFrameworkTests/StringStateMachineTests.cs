using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QiDiTu.StateMachineFramework.Attributes;
using QiDiTu.StateMachineFramework.Exceptions;

namespace QiDiTu.StateMachineFramework.Tests
{
    internal class Computer9 : StringStateMachine
    {
        [State(IsInitState = true)]
        private const string state1 = "asd";
        [State]
        public const string State2 = "ddd";

        [Translation(From = state1, To = State2)]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static void Test()
        {
            throw new Exception("asdadssdad");
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [Action("asd", ActionType.EnterState), Translation(From = State2, To = State2)]
        private void Testdsadad()
        {
        }
    }

    

    [TestClass]
    public class StringStateMachineTests
    {
        private class Computer : StringStateMachine
        {
            [State]
            private const string state1 = "asd";
        }

        [TestMethod]
        public void UnmarkInitStateTest()
        {
            Assert.ThrowsException<StateMachineInitException>(() => new Computer());
        }

        private class Computer1 : StringStateMachine
        {
            [State(IsInitState = true)]
            private const string state1 = "asdad";
            [State]
            private const int state2 = 333;
        }

        [TestMethod]
        public void ErrorStateTypeTest()
        {
            Assert.ThrowsException<StateMachineInitException>(() => new Computer1());
        }

        private class Computer2 : StringStateMachine
        {
            [State(IsInitState = true)]
            private const string state1 = "asdad";
            [State(IsInitState = true)]
            private const string state2 = "aaaa";
        }

        [TestMethod]
        public void MultiInitStateTest()
        {
            Assert.ThrowsException<StateMachineInitException>(() => new Computer2());
        }

        private class Computer3 : StringStateMachine
        {
            [State(IsInitState = true)]
            private const string Start = "Start";
            [State]
            private const string Running = "Running";
            [State]
            private const string EndState = "Shutdown";
        }

        
    }
}