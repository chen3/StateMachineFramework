using System;

namespace QiDiTu.StateMachineFramework.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// State machine mark action method attribute
    /// </summary>
    /// <remarks>
    ///     Mark method must return <see langwork="void"/> and 
    ///     paramater is <see cref="TranslationData"/> or <see langwork="void"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ActionAttribute : Attribute
    {
        /// <inheritdoc />
        /// <summary>
        /// Construction method
        /// </summary>
        /// <param name="state">The state of be listen. Not null.</param>
        /// <param name="type">
        ///     Action will be invoke when enter <paramref name="state"/> or leave <paramref name="state"/>.
        /// </param>
        public ActionAttribute(string state, StringStateMachine.ActionType type)
        {
            State = state;
            Type = type;
            WorkType = ActionWorkType.SignalStateEnterOrLeave;
        }

        /// <inheritdoc />
        /// <summary>
        /// Construction method
        /// </summary>
        /// <param name="fromState">
        ///     Action will be invoke when state changed 
        ///     from <paramref name="fromState"/> to <paramref name="toState"/>. Not null.
        /// </param>
        /// <param name="toState">
        ///     Action will be invoke when state changed 
        ///     from <paramref name="fromState"/> to <paramref name="toState"/>. Not null.
        /// </param>
        public ActionAttribute(string fromState = "", string toState = "")
        {
            Data = new TranslationData(fromState, toState);
            WorkType = ActionWorkType.SpecifiedStateSwitch;
        }

        public TranslationData Data { get; }

        public string State { get; }

        public StringStateMachine.ActionType Type { get; }

        public ActionWorkType WorkType { get; }

        public enum ActionWorkType
        {
            SpecifiedStateSwitch, SignalStateEnterOrLeave
        }

        public override string ToString()
        {
            if (WorkType == ActionWorkType.SignalStateEnterOrLeave)
            {
                return $"{nameof(ActionAttribute)}({nameof(State)}: {State}, {nameof(Type)}: {Type})";
            }
            string from = Data.From.Length == 0 ? "<Any>" : Data.From;
            string to = Data.To.Length == 0 ? "<Any>" : Data.To;
            return $"{nameof(ActionAttribute)}({nameof(Data.From)}: {@from}, {nameof(Data.To)}: {to})";
        }
    }
}