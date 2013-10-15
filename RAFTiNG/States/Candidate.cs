// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Candidate.cs" company="Cyrille DUPUYDAUBY">
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

namespace RAFTiNG.States
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using RAFTiNG.Messages;

    internal class Candidate<T> : State<T>
    {
        private IDictionary<string, GrantVote> voteReceived;

        /// <summary>
        /// Initializes a new instance of the <see cref="Candidate{T}"/> class.
        /// </summary>
        /// <param name="node">The node.</param>
        public Candidate(Node<T> node)
            : base(node)
        {
        }

        internal override void EnterState()
        {
            this.voteReceived = new Dictionary<string, GrantVote>();
            
            // increase term
            var nextTerm = this.Node.IncrementTerm();

            var candidateId = this.Node.Id;
            this.Node.State.VotedFor = candidateId;
            this.Node.LeaderId = string.Empty;

            // vote for self!
            this.RegisterVote(new GrantVote(true, candidateId, nextTerm));
            
            // send vote request
            this.Logger.TraceFormat("Broadcast a vote request");
            var request = new RequestVote(nextTerm, candidateId, this.Node.State.LastPersistedIndex, this.Node.State.LastPersistedTerm);
            this.Node.SendToOthers(request);
            this.ResetTimeout(.3);
        }

        internal override void ProcessVoteRequest(RequestVote request)
        {
            var currentTerm = this.CurrentTerm;
            if (request.Term > currentTerm)
            {
                this.Logger.DebugFormat(
                    "Received vote request from node with higher term ({0}'s term is {1}, our {2}). Resigning.",
                    request.CandidateId,
                    request.Term,
                    currentTerm);

                // we step down
                this.Node.SwitchToAndProcessMessage(NodeStatus.Follower, request);
                return;
            }

            if (request.Term < currentTerm && request.CandidateId != this.Node.Id)
            {
                // requesting a vote for a node that has less recent information
                // we decline
                this.Logger.TraceFormat("Received a vote request from a node with a lower term. We decline {0}", request);
            }
            else
            {
                /*// we check how complete is the log ?
                if (this.Node.State.LogIsBetterThan(request.LastLogTerm, request.LastLogIndex))
                {
                    // our log is better than the candidate's
                    vote = false;
                    this.Logger.TraceFormat("Received a vote request from a node with less information. We do not grant vote. Message: {0}.", request);
                }
                else */
                {
                    // we step down
                    this.Logger.DebugFormat("Received a vote request from a node with more information. We step down. Message: {0}.", request);
                    this.Node.SwitchToAndProcessMessage(NodeStatus.Follower, request);
                    return;
                }
            }

            // send back the response
            this.Node.SendMessage(request.CandidateId, new GrantVote(false, this.Node.Id, currentTerm));
        }

        internal override void ProcessVote(GrantVote vote)
        {
            if (vote.VoterTerm > this.CurrentTerm)
            {
                this.Node.State.CurrentTerm = vote.VoterTerm;
                this.Logger.DebugFormat("Received a vote from a node with a higher term. Dropping candidate status down. Message discarded {0}.", vote);
                this.Node.SwitchTo(NodeStatus.Follower);
                return;
            }

            if (this.voteReceived.ContainsKey(vote.VoterId))
            {
                // we already received a vote from the voter!
                this.Logger.WarnFormat(
                    "We received a second vote from {0}. Initial vote: {1}. Second vote: {2}.",
                    vote.VoterId,
                    this.voteReceived[vote.VoterId],
                    vote);
                return;
            }

            this.RegisterVote(vote);
        }

        internal override void ProcessAppendEntries(AppendEntries<T> appendEntries)
        {
            if (appendEntries.LeaderTerm >= this.CurrentTerm)
            {
                Logger.InfoFormat("Received AppendEntries from a probable leader, stepping down ({0}).", appendEntries);
                this.Node.SwitchToAndProcessMessage(NodeStatus.Follower, appendEntries);
            }
            else
            {
                Logger.DebugFormat("Received AppendEntries from an invalid leader, refusing.");
                var reply = new AppendEntriesAck(this.Node.Id, this.CurrentTerm, false);
                this.Node.SendMessage(appendEntries.LeaderId, reply);
            }
        }

        protected override void HeartbeatTimeouted(object state)
        {
            if (this.voteReceived.Count == this.Node.Settings.Nodes.Length)
            {
                this.Logger.DebugFormat("We got all votes back, but I am not elected.");
            }
            
            // no election, no leader, start a new election
            this.EnterState();
        }

        private void RegisterVote(GrantVote vote)
        {
            this.voteReceived.Add(vote.VoterId, vote);

            // count votes
            var votes = this.voteReceived.Values.Count(grantVote => grantVote.VoteGranted);

            if (votes < this.Node.Settings.Majority)
            {
                return;
            }

            var nodes = new StringBuilder();
            foreach (var key in this.voteReceived.Keys)
            {
                nodes.AppendFormat("{0},", key);
            }

            // we have a majority
            this.Logger.DebugFormat("I am your leader by {0}.", nodes);
            this.Node.SwitchTo(NodeStatus.Leader);
        }
    }
}