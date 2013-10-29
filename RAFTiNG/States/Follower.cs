//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="Follower.cs" company="Cyrille DUPUYDAUBY">
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
namespace RAFTiNG.States
{
    using RAFTiNG.Messages;

    /// <summary>
    /// Implements the follower behavior.
    /// </summary>
    /// <typeparam name="T">Type of command for the inner state machine.</typeparam>
    internal class Follower<T> : State<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Follower{T}"/> class.
        /// </summary>
        /// <param name="node">The node.</param>
        public Follower(Node<T> node)
            : base(node)
        {
        }

        internal override void EnterState()
        {
            // set timeout with 20% variability
            // if no sign of leader before timeout, we assume an election is required
            this.ResetTimeout(.2);
        }

        internal override void ProcessVoteRequest(RequestVote request)
        {
            bool vote;
            var currentTerm = this.CurrentTerm;
            if (request.Term < currentTerm)
            {
                // requesting a vote for a node that has less recent information
                // we decline
                if (this.Logger.IsTraceEnabled())
                {
                    this.Logger.TraceFormat("Vote request from node with lower term. Declined {0}.", request);
                }

                vote = false;
            }
            else
            {
                if (request.Term > currentTerm)
                {
                    if (this.Logger.IsDebugEnabled)
                    {
                        this.Logger.DebugFormat(
                            "Vote request from node with higher term. Updating our term. {0}",
                            request);
                    }

                    // we need to upgrade our term
                    this.Node.State.CurrentTerm = request.Term;
                }

                // we check how complete is the log ?
                if (this.Node.State.LogIsBetterThan(request.LastLogTerm, request.LastLogIndex))
                {
                    // our log is better than the candidate's
                    vote = false;
                    if (this.Logger.IsTraceEnabled())
                    {
                        this.Logger.TraceFormat(
                            "Vote request from node with less information. We do not vote. Message: {0}.",
                            request);
                    }
                }
                else if (string.IsNullOrEmpty(this.Node.State.VotedFor)
                    || this.Node.State.VotedFor == request.CandidateId)
                {
                    // grant vote
                    if (this.Logger.IsTraceEnabled())
                    {
                        this.Logger.TraceFormat(
                            "We do vote for node {1}. Message: {0}.", request, request.CandidateId);
                    }

                    vote = true;
                    this.Node.State.VotedFor = request.CandidateId;
                    
                    // as we did vote, we are ok to wait longer
                    this.ResetTimeout(0, 2);
                }
                else
                {
                    // we already voted for someone
                    vote = false;
                    if (this.Logger.IsTraceEnabled())
                    {
                        this.Logger.TraceFormat(
                            "We already voted. We do not grant vote. Message: {0}.", request);
                    }
                }
            }
            
            // send back the response
            this.Node.SendMessage(request.CandidateId, new GrantVote(vote, this.Node.Id, currentTerm));
        }

        internal override void ProcessVote(GrantVote vote)
        {
            if (this.Logger.IsDebugEnabled)
            {
                this.Logger.DebugFormat(
                    "Received a vote but I am a follower. Message discarded: {0}.", vote);
            }
        }

        internal override void ProcessAppendEntries(AppendEntries<T> appendEntries)
        {
            bool result;
            if (appendEntries.LeaderTerm < this.CurrentTerm)
            {
                // leader is older than us or log does not match
                this.Logger.DebugFormat(
                    "Reject an AppendEntries from an invalid leader ({0}).", appendEntries);
                result = false;
            }
            else
            {
                // we will proceed 
                this.Node.LeaderId = appendEntries.LeaderId;
                if (appendEntries.LeaderTerm > this.CurrentTerm)
                {
                    this.Logger.TraceFormat("Upgrade our term to {0}.", this.CurrentTerm);
                    this.Node.State.CurrentTerm = appendEntries.LeaderTerm;
                }

                if (this.Node.State.EntryMatches(
                    appendEntries.PrevLogIndex, appendEntries.PrevLogTerm))
                {
                    this.Logger.TraceFormat(
                        "Process an AppendEntries request: {0}", appendEntries);
                    this.Node.State.AppendEntries(appendEntries.PrevLogIndex, appendEntries.Entries);
                    this.Node.Commit(appendEntries.CommitIndex);
                    result = true;
                }
                else
                {
                    // log does not match, we are not in sync with leader yet
                    this.Logger.DebugFormat(
                            "Reject an AppendEntries that does not match our log ({0}).",
                            appendEntries);
                    result = false;
                }
            }

            var reply = new AppendEntriesAck(this.Node.Id, this.CurrentTerm, result);
            this.Node.SendMessage(appendEntries.LeaderId, reply);
            this.ResetTimeout(.2);
        }

        protected override void HeartbeatTimeouted(object state)
        {
            if (this.Done)
            {
                // this state is no longer active
                return;
            }

            this.Logger.Info(
                    "Timeout elapsed without sign from current leader. Trigger an election.");

            // heartBeat timeout, we will trigger an election.
            this.Node.SwitchTo(NodeStatus.Candidate);
        }
    }
}