﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PersistedState.cs" company="Cyrille DUPUYDAUBY">
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
    using System.Collections.Generic;

    /// <summary>
    /// Describes the persisted state of a node.
    /// </summary>
    /// <typeparam name="T">Command type for the internal state machine</typeparam>
    public class PersistedState<T>
    {
        #region properties

        private long currentTerm;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedState{T}"/> class.
        /// </summary>
        public PersistedState()
        {
            this.LogEntries = new List<LogEntry<T>>();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the last known term.
        /// </summary>
        public long CurrentTerm
        {
            get
            {
                return this.currentTerm;
            }

            set
            {
                if (this.currentTerm == value)
                {
                    return;
                }

                this.currentTerm = value;
                this.VotedFor = null;
            }
        }

        /// <summary>
        /// Gets the last persisted term.
        /// </summary>
        /// <value>
        /// The last persisted term.
        /// </value>
        public long LastPersistedTerm
        {
            get
            {
                return this.LogEntries.Count > 0
                           ? this.LogEntries[this.LogEntries.Count - 1].Term
                           : 0;
            }
        }

        /// <summary>
        /// Gets the last index of the persisted.
        /// </summary>
        /// <value>
        /// The last index of the persisted.
        /// </value>
        public int LastPersistedIndex
        {
            get
            {
                return this.LogEntries.Count > 0 ? this.LogEntries.Count - 1 : -1;
            }
        }

        /// <summary>
        /// Gets or sets the name of the node we voted for (null is none).
        /// </summary>
        public string VotedFor { get; set; }

        /// <summary>
        /// Gets the persisted command.
        /// </summary>
        public IList<LogEntry<T>> LogEntries { get; private set; }

        #endregion

        #region methods

        /// <summary>
        /// Adds the entry.é
        /// </summary>
        /// <param name="logEntry">The log entry.</param>
        public void AddEntry(LogEntry<T> logEntry)
        {
            if (logEntry.Term == 0)
            {
                var newEntry = new LogEntry<T>(logEntry.Command, this.CurrentTerm);
                this.LogEntries.Add(newEntry);
            }
            else
            {
                this.LogEntries.Add(logEntry);
            }
        }

        /// <summary>
        /// Adds the entry.
        /// </summary>
        /// <param name="command">The command.</param>
        public void AddEntry(T command)
        {
            var newEntry = new LogEntry<T>(command, this.currentTerm);
            this.LogEntries.Add(newEntry);
        }

        /// <summary>
        /// Checks if our log is better than the given criteria
        /// </summary>
        /// <param name="lastLogTerm">The last log term.</param>
        /// <param name="lastLogIndex">Last index of the log.</param>
        /// <returns>True if our log contains entries of a greater term or if we have more entries and the same term.</returns>
        /// <remarks>See RAFT specification.</remarks>
        public bool LogIsBetterOrSameAs(long lastLogTerm, long lastLogIndex)
        {
            if (this.LogEntries.Count == 0)
            {
                // no log, we are the worst
                return false;
            }

            var lastEntryId = this.LogEntries.Count - 1;
            var lastEntry = this.LogEntries[lastEntryId];
            if (lastEntry.Term > lastLogTerm)
            {
                // if we have more recent info
                return true;
            }

            if (lastEntry.Term < lastLogTerm)
            {
                return false;
            }

            return lastEntryId > lastLogIndex;
        }

        /// <summary>
        /// Check if a given entry has the appropriate characteristics.
        /// </summary>
        /// <param name="index">The requested index.</param>
        /// <param name="term">The expected term.</param>
        /// <returns>true if the entry at index <paramref name="index"/> has the term <paramref name="term"/>.</returns>
        public bool EntryMatches(int index, long term)
        {
            if (index >= this.LogEntries.Count || index < 0)
            {
                return index == -1;
            }

            return this.LogEntries[index].Term == term;
        }

        /// <summary>
        /// Apply a set of <see cref="LogEntry{T}"/> to this log.
        /// </summary>
        /// <param name="prevLogIndex">first index to replace/add</param>
        /// <param name="entries">List of log entries.</param>
        public void AppendEntries(int prevLogIndex, IEnumerable<LogEntry<T>> entries)
        {
            if (entries == null)
            {
                // nothing to add
                return;
            }

            prevLogIndex++;
            foreach (var logEntry in entries)
            {
                if (prevLogIndex == this.LogEntries.Count)
                {
                    this.LogEntries.Add(logEntry);
                }
                else
                {
                    this.LogEntries[prevLogIndex] = logEntry;
                }

                prevLogIndex++;
            }
        }

        #endregion
    }
}