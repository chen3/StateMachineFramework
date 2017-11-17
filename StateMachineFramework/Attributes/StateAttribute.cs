using System;

namespace QiDiTu.StateMachineFramework.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// Mark the field is state
    /// </summary>
    /// <see cref="P:QiDiTu.StateMachineFramework.StringStateMachine.StateFieldType" />
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class StateAttribute : Attribute
    {
        /// <summary>
        /// Set the state is initialization state.
        /// </summary>
        /// <remarks>Only set one in a state machine.</remarks>
        public bool IsInitState { get; set; } = false;

        public override string ToString()
        {
            return $"{nameof(StateAttribute)}({nameof(IsInitState)}: {IsInitState})";
        }
    }
}
