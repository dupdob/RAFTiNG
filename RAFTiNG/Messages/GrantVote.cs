//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="GrantVote.cs" company="Cyrille DUPUYDAUBY">
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
    /// Describes the result of a RequestVote Message.
    /// </summary>
    public class GrantVote
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrantVote"/> class.
        /// </summary>
        /// <param name="voteGranted">if set to <c>true</c> [vote granted].</param>
        /// <param name="voterId">The voter id.</param>
        /// <param name="voterTerm">The voter term.</param>
        public GrantVote(bool voteGranted, string voterId, long voterTerm)
        {
            this.VoteGranted = voteGranted;
            this.VoterId = voterId;
            this.VoterTerm = voterTerm;
        }

        /// <summary>
        /// Gets a value indicating whether the vote was granted].
        /// </summary>
        /// <value>
        ///   <c>true</c> if vote granted; otherwise, <c>false</c>.
        /// </value>
        public bool VoteGranted { get; private set; }

        /// <summary>
        /// Gets the voter id.
        /// </summary>
        /// <value>
        /// The voter id.   
        /// </value>
        /// <remarks>The voter ID is the sender id.</remarks>
        public string VoterId { get; private set; }

        /// <summary>
        /// Gets the voter term.
        /// </summary>
        /// <value>
        /// The voter term.
        /// </value>
        public long VoterTerm { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "GrantVode: Voter {1} {0} (VoterTerm: {2})",
                this.VoteGranted ? "voted" : "did not vote",
                this.VoterId,
                this.VoterTerm);
        }
    }
}