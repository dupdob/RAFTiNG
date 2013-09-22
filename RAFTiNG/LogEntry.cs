// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogEntry.cs" company="Cyrille DUPUYDAUBY">
//   Copyright 2013 Cyrille DUPUYDAUBY
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//   http://www.apache.org/licenses/LICENSE-2.0
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// <summary>
//   
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RAFTiNG
{
    /// <summary>
    /// A log entry description
    /// </summary>
    /// <typeparam name="T">Command for the internal state machine.</typeparam>
    public sealed class LogEntry<T>
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
        public LogEntry(T command, long term)
        {
            this.Command = command;
            this.Term = term;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry{T}"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        public LogEntry(T command)
        {
            this.Command = command;
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

        #endregion

        #region methods

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
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
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return this.Term.GetHashCode();
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="other">
        /// The <see cref="System.Object"/> to compare with this instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        private bool Equals(LogEntry<T> other)
        {
            return this.Term == other.Term;
        }

        #endregion
    }
}