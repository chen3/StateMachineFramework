using System;

namespace QiDiTu.StateMachineFramework.Exceptions
{
    public class StateMachineException : Exception
    {
        public StateMachineException()
        {
        }

        public StateMachineException(string message)
            : base(message)
        {
        }

        public StateMachineException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}