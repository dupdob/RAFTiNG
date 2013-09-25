//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="Leader.cs" company="Cyrille DUPUYDAUBY">
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
    using System;
    using System.Collections.Generic;

    using RAFTiNG.Messages;

    internal class Leader<T> : State<T>
    {
        private readonly Dictionary<string, FollowerLogState> states = new Dictionary<string, FollowerLogState>();

        public Leader(Node<T> node)
            : base(node)
        {
        }

        internal override void EnterState()
        {
            // keep track of followers log state
            foreach (var otherNode in this.Node.Settings.OtherNodes())
            {
                this.states[otherNode] = new FollowerLogState(this.Node.State.LastPersistedIndex, false);
            }

            this.BroadcastHeartbeat();
        }

        internal override void ProcessVoteRequest(RequestVote request)
        {
            GrantVote response;
            if (request.Term <= this.CurrentTerm)
            {
                // requesting a vote for a node that has less recent information
                // we decline
                this.Logger.TraceFormat(
                    "Received a vote request from a node with a lower term. Message discarded {0}",
                    request);
                response = new GrantVote(false, this.Node.Address, this.CurrentTerm);
            }
            else
            {
                if (request.Term > this.CurrentTerm)
                {
                    this.Logger.DebugFormat(
                        "Received a vote request from a node with a higher term ({0}'s term is {1}, our {2}). Stepping down.",
                        request.CandidateId,
                        request.Term,
                        this.Node.State.CurrentTerm);

                    // we step down
                    this.Node.SwitchToAndProcessMessage(NodeStatus.Follower, request);
                    return;
                }

                response = new GrantVote(false, this.Node.Address, this.CurrentTerm);
            }

            // send back the response
            this.Node.SendMessage(request.CandidateId, response);
        }

        internal override void ProcessVote(GrantVote vote)
        {
            this.Logger.TraceFormat(
                "Received a vote but we are no longer interested: {0}",
                vote);
        }

        internal override void ProcessAppendEntries(AppendEntries<T> appendEntries)
        {
            if (appendEntries.LeaderTerm >= this.CurrentTerm)
            {
                Logger.InfoFormat("Received AppendEntries from a probable leader, stepping down.");
                this.Node.SwitchToAndProcessMessage(NodeStatus.Follower, appendEntries);
            }
            else
            {
                Logger.DebugFormat("Received AppendEntries from an invalid leader, refusing.");
                var reply = new AppendEntriesAck(this.Node.Address, this.CurrentTerm, false);
                this.Node.SendMessage(appendEntries.LeaderId, reply);
            }
        }

        protected internal override void ProcessAppendEntriesAck(AppendEntriesAck appendEntriesAck)
        {
            int entriesToSend;
            var followerId = appendEntriesAck.NodeId;
            var followerLogState = this.states[followerId];
            if (!appendEntriesAck.Success)
            {
                followerLogState.LastSentIndex--;
                var message = new AppendEntries<T>
                {
                    LeaderId = this.Node.Address,
                    LeaderTerm = this.CurrentTerm,
                    PrevLogIndex = followerLogState.LastSentIndex,
                    PrevLogTerm = followerLogState.LastSentIndex == -1 ? 0 : this.Node.State.LogEntries[followerLogState.LastSentIndex].Term,
                    CommitIndex = 0
                };
                this.Node.SendMessage(followerId, message);
            }
            else
            {
                entriesToSend = Math.Max(
                    0,
                    Math.Min(10, this.Node.State.LastPersistedIndex - followerLogState.LastSentIndex));
                if (entriesToSend == 0)
                {
                    return;
                }

                var message = new AppendEntries<T>
                {
                    LeaderId = this.Node.Address,
                    LeaderTerm = this.CurrentTerm,
                    PrevLogIndex = followerLogState.LastSentIndex,
                    PrevLogTerm = followerLogState.LastSentIndex == -1 ? 0 : this.Node.State.LogEntries[followerLogState.LastSentIndex].Term,
                    Entries = new LogEntry<T>[entriesToSend],
                    CommitIndex = 0
                };

                for (var i = 0; i < entriesToSend; i++)
                {
                    followerLogState.LastSentIndex++;
                    message.Entries[i] = this.Node.State.LogEntries[followerLogState.LastSentIndex];
                }

                this.Node.SendMessage(followerId, message);
            }
        }

        protected override void HeartbeatTimeouted(object state)
        {
            // send keep alive
            this.BroadcastHeartbeat();
        }

        private void BroadcastHeartbeat()
        {
            foreach (var entry in this.states)
            {
                var followerLogState = entry.Value;

                if (followerLogState.LastSentIndex == this.Node.State.LastPersistedIndex)
                {
                    var folllowerId = entry.Key;
                    var message = new AppendEntries<T>
                    {
                        LeaderId = this.Node.Address,
                        LeaderTerm = this.CurrentTerm,
                        PrevLogIndex = followerLogState.LastSentIndex,
                        PrevLogTerm =
                            followerLogState.LastSentIndex == -1
                                ? 0
                                : this.Node.State.LogEntries[
                                    followerLogState.LastSentIndex].Term,
                        CommitIndex = 0
                    };
                    this.Node.SendMessage(folllowerId, message);
                }
            }

            this.ResetTimeout(0, .5);
        }

        private class FollowerLogState
        {
            public int LastSentIndex { get; set; }

            public bool SynchroEstablished { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="FollowerLogState"/> class.
            /// </summary>
            /// <param name="lastSentIndex">Last index of the sent.</param>
            /// <param name="synchroEstablished">if set to <c>true</c> [synchro established].</param>
            public FollowerLogState(int lastSentIndex, bool synchroEstablished)
            {
                this.LastSentIndex = lastSentIndex;
                this.SynchroEstablished = synchroEstablished;
            }
        }
    }
}