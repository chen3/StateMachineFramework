using System;

namespace QiDiTu.StateMachineFramework.Exceptions
{
    public class StateNotFoundException : SwitchStateException
    {
        public StateNotFoundException()
        {
        }

        public string State { get; }

        public StateNotFoundException(string state)
            : base($"State {state} not found")
        {
            State = state;
        }

        public StateNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public StateNotFoundException(string state, string message)
            : base(message)
        {
            State = state;
        }
    }
}