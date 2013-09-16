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
            this.Logger.DebugFormat("Set timeout to {0} ms", this.Node.TimeOutInMs);
            this.ResetTimeout(.1);
        }

        internal override void ProcessVoteRequest(RequestVote request)
        {
            bool vote;
            var currentTerm = this.CurrentTerm;
            if (request.Term <= currentTerm)
            {
                // requesting a vote for a node that has less recent information
                // we decline
                this.Logger.TraceFormat("Received a vote request from a node with a lower term. We decline {0}", request);
                vote = false;
            }
            else
            {
                if (request.Term > currentTerm)
                {
                    this.Logger.DebugFormat(
                        "Received a vote request from a node with a higher term ({0}'s term is {1}, our {2}). Updating our term.",
                        request.CandidateId,
                        request.Term,
                        currentTerm);

                    // we need to upgrade our term
                    this.Node.State.CurrentTerm = request.Term;
                }

                // we check how complete is the log ?
                if (this.Node.State.LogIsBetterThan(request.LastLogTerm, request.LastLogIndex))
                {
                    // our log is better than the candidate's
                    vote = false;
                    this.Logger.TraceFormat("Received a vote request from a node with less information. We do not grant vote. Message: {0}.", request);
                }
                else if (string.IsNullOrEmpty(this.Node.State.VotedFor)
                    || this.Node.State.VotedFor == request.CandidateId)
                {
                    // grant vote
                    this.Logger.TraceFormat("We do grant vote. Message: {0}.", request);
                    vote = true;
                }
                else
                {
                    // we already voted for someone
                    vote = false;
                    this.Logger.TraceFormat("We already voted. We do not grant vote. Message: {0}.", request);
                }
            }
            
            if (vote)
            {
                this.Node.State.VotedFor = request.CandidateId;
                this.ResetTimeout(.1);
            }

            // send back the response
            this.Node.SendMessage(request.CandidateId, new GrantVote(vote, this.Node.Address, currentTerm));
        }

        internal override void ProcessVote(GrantVote vote)
        {
            this.Logger.WarnFormat(
                "Received a vote but I am a follower. Message discarded: {0}.", vote);
        }

        protected override void HeartbeatTimeouted(object state)
        {
            if (this.Done)
            {
                // this state is no longer active
                return;
            }

            this.Logger.Warn("Timeout elapsed without sign from current leader.");

            this.Logger.Info("Trigger an election.");

            // heartBeat timeout, we will trigger an election.
            this.Node.SwitchTo(NodeStatus.Candidate);
        }
    }
}