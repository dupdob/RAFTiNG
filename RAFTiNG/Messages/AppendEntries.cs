//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppendEntries.cs" company="Cyrille DUPUYDAUBY">
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
//  --------------------------------------------------------------------------------------------------------------------
namespace RAFTiNG.Messages
{
    /// <summary>
    /// Message send by the leader to follower so they store entries.
    /// </summary>
    /// <typeparam name="T">Business automaton message type.</typeparam>
    public class AppendEntries<T>
    {
        #region fields

        private long leaderTerm;

        private string leaderId;

        private int prevLogIndex;

        private long prevLogTerm;

        private LogEntry<T>[] entries;

        private int commitIndex;

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the leader term.
        /// </summary>
        /// <value>
        /// The leader term.
        /// </value>
        public long LeaderTerm
        {
            get
            {
                return this.leaderTerm;
            }

            set
            {
                this.leaderTerm = value;
            }
        }

        /// <summary>
        /// Gets or sets the leader id.
        /// </summary>
        /// <value>
        /// The leader id.
        /// </value>
        public string LeaderId
        {
            get
            {
                return this.leaderId;
            }

            set
            {
                this.leaderId = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the previous log.
        /// </summary>
        /// <value>
        /// The index of the previous log.
        /// </value>
        public int PrevLogIndex
        {
            get
            {
                return this.prevLogIndex;
            }

            set
            {
                this.prevLogIndex = value;
            }
        }

        /// <summary>
        /// Gets or sets the previous log term.
        /// </summary>
        /// <value>
        /// The previous log term.
        /// </value>
        public long PrevLogTerm
        {
            get
            {
                return this.prevLogTerm;
            }

            set
            {
                this.prevLogTerm = value;
            }
        }

        /// <summary>
        /// Gets or sets the entries.
        /// </summary>
        /// <value>
        /// The entries.
        /// </value>
        public LogEntry<T>[] Entries
        {
            get
            {
                return this.entries;
            }

            set
            {
                this.entries = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the commit.
        /// </summary>
        /// <value>
        /// The index of the commit.
        /// </value>
        public int CommitIndex
        {
            get
            {
                return this.commitIndex;
            }

            set
            {
                this.commitIndex = value;
            }
        }

        #endregion

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return
                string.Format(
                    "AppendEntries: LeaderId: {1} (Term {0}), PrevLog Index and Term: {2}, {3}, Commit {4}, Entries: {5}",
                    this.leaderTerm,
                    this.leaderId,
                    this.prevLogIndex,
                    this.prevLogTerm,
                    this.commitIndex,
                    this.entries == null ? 0 : this.entries.Length);
        }
    }
}