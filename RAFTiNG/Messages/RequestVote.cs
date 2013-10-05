// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequestVote.cs" company="Cyrille DUPUYDAUBY">
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

namespace RAFTiNG.Messages
{
    /// <summary>
    /// Describes a vote request emitted by a candidate.
    /// </summary>
    public class RequestVote
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestVote"/> class.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <param name="candidateId">The candidate id.</param>
        /// <param name="lastLogIndex">Last index of the log.</param>
        /// <param name="lastLogTerm">The last log term.</param>
        public RequestVote(long term, string candidateId, long lastLogIndex, long lastLogTerm)
        {
            this.Term = term;
            this.CandidateId = candidateId;
            this.LastLogIndex = lastLogIndex;
            this.LastLogTerm = lastLogTerm;
        }

        /// <summary>
        /// Gets the term for the election.
        /// </summary>
        /// <value>
        /// The term.
        /// </value>
        public long Term { get; private set; }

        /// <summary>
        /// Gets the candidate id requesting the vote.
        /// </summary>
        /// <value>
        /// The candidate id.
        /// </value>
        public string CandidateId { get; private set; }

        /// <summary>
        /// Gets the last index of the log of the candidate.
        /// </summary>
        /// <value>
        /// The last index of the log.
        /// </value>
        public long LastLogIndex { get; private set; }

        /// <summary>
        /// Gets the last log term of the candidate.
        /// </summary>
        /// <value>
        /// The last log term.
        /// </value>
        public long LastLogTerm { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "RequestVote: Candidate: {1} (Term {0}, LastLogEntry Index and Term: {2}, {3})",
                this.Term,
                this.CandidateId,
                this.LastLogIndex,
                this.LastLogTerm);
        }
    }
}