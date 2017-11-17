using System;

namespace QiDiTu.StateMachineFramework.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// State machine mark translation method attribute
    /// </summary>
    /// <remarks>
    ///     Mark method must return <see langwork="void"/> and 
    ///     paramater is <see cref="TranslationData"/> or <see langwork="void"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TranslationAttribute : Attribute
    {
        public string From { get; set; } = string.Empty;

        public string To { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{nameof(TranslationAttribute)}({nameof(From)}: {From ?? "<Any>"}, {nameof(To)}: {To ?? "<Any>"})";
        }
    }
}