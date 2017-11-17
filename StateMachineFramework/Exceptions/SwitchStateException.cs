using System;

namespace QiDiTu.StateMachineFramework.Exceptions
{
    public class SwitchStateException : StateMachineException
    {
        public SwitchStateException()
        {
        }

        public SwitchStateException(string from, string to)
            : base($"State can't switch from {from} to {to}")
        {
        }

        public SwitchStateException(TranslationData data)
            : base($"State can't switch from {data.From} to {data.To}")
        {
        }

        public SwitchStateException(string from, string to, string message)
            : base($"State can't switch from {from} to {to}, {message}")
        {
        }

        public SwitchStateException(TranslationData data, string message)
            : base($"State can't switch from {data.From} to {data.To}, {message}")
        {
        }

        public SwitchStateException(string message)
            : base(message)
        {
        }

        public SwitchStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}