using System;

namespace QiDiTu.StateMachineFramework.Exceptions
{
    public class StateMachineInitException : StateMachineException
    {
        public StateMachineInitException()
        {
        }

        public StateMachineInitException(string message)
            : base(message)
        {
        }

        public StateMachineInitException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}