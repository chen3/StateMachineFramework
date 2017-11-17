using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace QiDiTu.StateMachineFramework
{
    /// <summary>
    /// Store state name of From and To
    /// </summary>
    [SuppressMessage("ReSharper", "InheritdocConsiderUsage")]
    public struct TranslationData : ICloneable
    {
        /// <summary>
        /// Construction method
        /// </summary>
        /// <param name="from">Translation from.</param>
        /// <param name="to">Translation to.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="from"/> or <paramref name="to"/> is <see langwork="null"/>.
        /// </exception>
        public TranslationData(string from = "", string to = "")
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }

        public string From { get; }

        public string To { get; }
        
        public override int GetHashCode()
        {
            return From?.GetHashCode() ?? 0 + To?.GetHashCode() ?? 0;
        }

        public object Clone()
        {
            return new TranslationData(From, To);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TranslationData))
            {
                return false;
            }
            var data = (TranslationData) obj;
            return From == data.From && To == data.To;
        }

        public override string ToString()
        {
            return $"{nameof(TranslationData)}({nameof(From)}: {From}, {nameof(To)}: {To})";
        }
    }
}