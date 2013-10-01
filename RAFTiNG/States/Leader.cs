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
    using System.Linq;

    using RAFTiNG.Messages;

    internal class Leader<T> : State<T>
    {
        private readonly Dictionary<string, LogReplicationAgent> states = new Dictionary<string, LogReplicationAgent>();

        public Leader(Node<T> node)
            : base(node)
        {
        }

        internal override void EnterState()
        {
            // keep track of followers log state
            foreach (var otherNode in this.Node.Settings.OtherNodes())
            {
// ReSharper disable PossibleLossOfFraction
                this.states[otherNode] = new LogReplicationAgent(TimeSpan.FromMilliseconds(this.Node.Settings.TimeoutInMs / 2), this.Node.State.LogEntries.Count);
// ReSharper restore PossibleLossOfFraction
            }

            this.BroadcastHeartbeat();
        }

        // deal with vote request
        internal override void ProcessVoteRequest(RequestVote request)
        {
            if (request.Term > this.CurrentTerm)
            {
                this.Logger.DebugFormat(
                    "Received a vote request from a node with a higher term ({0}'s term is {1}, our {2}). Stepping down and process vote.",
                    request.CandidateId,
                    request.Term,
                    this.Node.State.CurrentTerm);

                // we step down
                this.Node.SwitchToAndProcessMessage(NodeStatus.Follower, request);
                return;
            }

            // requesting a vote for a node that has less recent information
            // we decline
            this.Logger.TraceFormat(
                "Received a vote request from a node with a lower term, we refuse (Msg : {0})",
                request);
            var response = new GrantVote(false, this.Node.Address, this.CurrentTerm);

            // send back the response
            this.Node.SendMessage(request.CandidateId, response);
        }

        // process vote
        internal override void ProcessVote(GrantVote vote)
        {
            this.Logger.TraceFormat(
                "Received a vote but we are no longer interested: {0}",
                vote);
        }

        // process appendentries
        internal override void ProcessAppendEntries(AppendEntries<T> appendEntries)
        {
            if (appendEntries.LeaderTerm >= this.CurrentTerm)
            {
                Logger.InfoFormat("Received AppendEntries from a probable leader, stepping down.");
                this.Node.SwitchToAndProcessMessage(NodeStatus.Follower, appendEntries);
                return;
            }

            this.Logger.DebugFormat("Received AppendEntries from an invalid leader, refusing.");
            var reply = new AppendEntriesAck(this.Node.Address, this.CurrentTerm, false);
            this.Node.SendMessage(appendEntries.LeaderId, reply);
        }

        // processes ack for appenentries
        // keeps track of other nodes
        // update commit index
        internal override void ProcessAppendEntriesAck(AppendEntriesAck appendEntriesAck)
        {
            var followerId = appendEntriesAck.NodeId;
            var followerLogState = this.states[followerId];
            followerLogState.ProcessAppendEntriesAck(appendEntriesAck.Success);
            var message = followerLogState.GetAppendEntries(this.Node.State.LogEntries);
            if (appendEntriesAck.Success)
            {
                this.UpdateCommitIndex();                
            }

            if (message == null)
            {
                return;
            }

            message.LeaderId = this.Node.Address;
            message.LeaderTerm = this.CurrentTerm;
            this.Node.SendMessage(followerId, message);
        }

        protected override void HeartbeatTimeouted(object state)
        {
            // send keep alive
            this.BroadcastHeartbeat();
        }

        // compute the new commiindex
        private void UpdateCommitIndex()
        {
            var ordered = this.states.Values.Select(state => state.MinSynchronizedIndex).OrderBy(value => value);
            var index = this.Node.Settings.Nodes.Length - this.Node.Settings.Majority + 1;
            this.Node.State.CommitIndex = ordered.ElementAt(index);
            Logger.TraceFormat("Commit index is now {0}.", this.Node.State.CommitIndex);
        }

        private void BroadcastHeartbeat()
        {
            foreach (var entry in this.states)
            {
                var followerLogState = entry.Value;
                var message = followerLogState.GetAppendEntries(this.Node.State.LogEntries);
                if (message != null)
                {
                    message.LeaderId = this.Node.Address;
                    message.LeaderTerm = this.CurrentTerm;
                    this.Node.SendMessage(entry.Key, message);
                }
            }

            this.ResetTimeout(0, .5);
        }

        private class LogReplicationAgent
        {
            #region fields

            private const int MaxBatch = 20;

            private readonly TimeSpan maxDelay;

            private int minSynchronizedIndex;

            private int lastSentIndex = -1;

            private bool flyingTransaction;

            private DateTime lastSentMessageTime;

            #endregion

            /// <summary>
            /// Initializes a new instance of the <see cref="LogReplicationAgent" /> class.
            /// </summary>
            /// <param name="maxDelay">The max delay between two messages.</param>
            /// <param name="logSize">Size of the log.</param>
            public LogReplicationAgent(TimeSpan maxDelay, int logSize)
            {
                this.maxDelay = maxDelay;
                this.minSynchronizedIndex = logSize - 1;
                this.lastSentMessageTime = DateTime.Now;
            }

            /// <summary>
            /// Gets the index of the min synchronized.
            /// </summary>
            /// <value>
            /// The index of the min synchronized.
            /// </value>
            public int MinSynchronizedIndex
            {
                get
                {
                    return this.minSynchronizedIndex;
                }
            }

            /// <summary>
            /// Gets a value indicating whether the delay between messages elapsed.
            /// </summary>
            /// <value>
            ///   <c>true</c> if delay elapsed; otherwise, <c>false</c>.
            /// </value>
            private bool DelayElapsed
            {
                get
                {
                    return (DateTime.Now - this.lastSentMessageTime) >= this.maxDelay;
                }
            }

            /// <summary>
            /// Gets the next <see cref="AppendEntries{T}"/> message to be sent.
            /// </summary>
            /// <param name="log">Entry log to keep in synchronization.
            /// </param>
            /// <returns>
            /// The message to be send, null if none.
            /// </returns>
            public AppendEntries<T> GetAppendEntries(IList<LogEntry<T>> log)
            {
                // if we are waiting for an answer and delay is not elapsed
                // we do nothing
                if (this.flyingTransaction)
                {
                    return null;
                }

                var entriesToSend = Math.Max(
                    0, Math.Min(MaxBatch, log.Count - this.minSynchronizedIndex - 1));

                if (entriesToSend == 0 && !this.DelayElapsed)
                {
                    return null;
                }

                this.flyingTransaction = true;
                var message = new AppendEntries<T> { PrevLogIndex = this.minSynchronizedIndex };
                try
                {
                    message.PrevLogTerm = this.minSynchronizedIndex < 0 ? -1 : log[this.minSynchronizedIndex].Term;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                message.Entries = new LogEntry<T>[entriesToSend];
                var offset = this.minSynchronizedIndex + 1;
                for (var i = 0; i < entriesToSend; i++)
                {
                    message.Entries[i] = log[i + offset];
                }

                this.lastSentIndex = offset + entriesToSend - 1;
                this.lastSentMessageTime = DateTime.Now;
                return message;
            }

            /// <summary>
            /// Processes the append entries acknowledgement message.
            /// </summary>
            /// <param name="success">if set to <c>true</c> [success].</param>
            public void ProcessAppendEntriesAck(bool success)
            {
                this.flyingTransaction = false;
                if (success)
                {
                    // if everything was ok
                    this.minSynchronizedIndex = this.lastSentIndex;
                    return;
                }

                // it fails, log is not synchronised
                this.minSynchronizedIndex--;
            }
        }
    }
}