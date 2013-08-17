// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="LogEntry.cs" company="">
// //   Copyright 2013 Cyrille DUPUYDAUBY
// //   Licensed under the Apache License, Version 2.0 (the "License");
// //   you may not use this file except in compliance with the License.
// //   You may obtain a copy of the License at
// //       http://www.apache.org/licenses/LICENSE-2.0
// //   Unless required by applicable law or agreed to in writing, software
// //   distributed under the License is distributed on an "AS IS" BASIS,
// //   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //   See the License for the specific language governing permissions and
// //   limitations under the License.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------
namespace RAFTiNG
{
    using System.Runtime.Serialization;

    /// <summary>
    /// A log entry description
    /// </summary>
    /// <typeparam name="T">Command for the internal state machine.</typeparam>
    public class LogEntry<T>
    {
        #region attributes

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry{T}" /> class.
        /// </summary>
        /// <param name="command">Command for the state machine to be stored.
        /// </param>
        /// <param name="term">Current term when command is stored.
        /// </param>
        /// <param name="index">Index for the current command.
        /// </param>
        public LogEntry(T command, long term, long index)
        {
            this.Command = command;
            this.Term = term;
            this.Index = index;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the stored command.
        /// </summary>
        public T Command { get; private set; }

        /// <summary>
        /// Gets the term for the command.
        /// </summary>
        public long Term { get; private set; }

        /// <summary>
        /// Gets the index for this command.
        /// </summary>
        public long Index { get; private set; }

        #endregion

        #region methods

        /// <summary>
        /// Détermine si l'objet <see cref="T:System.Object"/> spécifié est égal à l'objet <see cref="T:System.Object"/> actuel.
        /// </summary>
        /// <returns>
        /// true si l'objet spécifié est égal à l'objet actuel ; sinon, false.
        /// </returns>
        /// <param name="obj">Objet à comparer avec l'objet actif.</param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((LogEntry<T>)obj);
        }

        /// <summary>
        /// Sert de fonction de hachage pour un type particulier.
        /// </summary>
        /// <returns>
        /// Code de hachage du <see cref="T:System.Object"/> actuel.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Term.GetHashCode() * 397) ^ this.Index.GetHashCode();
            }
        }

        public static bool operator ==(LogEntry<T> left, LogEntry<T> right)
        {
            return LogEntry<T>.Equals(left, right);
        }

        public static bool operator !=(LogEntry<T> left, LogEntry<T> right)
        {
            return !LogEntry<T>.Equals(left, right);
        }

        protected bool Equals(LogEntry<T> other)
        {
            return this.Term == other.Term && this.Index == other.Index;
        }

        #endregion
    }
}