﻿//  --------------------------------------------------------------------------------------------------------------------
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

    using log4net;

    using RAFTiNG.Messages;

    using log4net.Core;

    internal class Leader<T> : State<T>
    {
        private readonly Dictionary<string, LogReplicationAgent> states = new Dictionary<string, LogReplicationAgent>();

        public Leader(Node<T> node)
            : base(node)
        {
        }

        internal override void EnterState()
        {
            this.Node.LeaderId = this.Node.Id;
            
            // keep track of followers log state
            foreach (var otherNode in this.Node.Settings.OtherNodes())
            {
                this.states[otherNode] =
                    new LogReplicationAgent(
                        TimeSpan.FromMilliseconds(this.Node.Settings.TimeoutInMs / 2.0),
                        this.Node.State.LogEntries.Count,
                        otherNode,
                        this.Logger);
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
            
            // send back the response
            this.Node.SendVote(request.CandidateId, false);
        }

        // process vote
        internal override void ProcessVote(GrantVote vote)
        {
            this.Logger.TraceFormat(
                "Received a vote but we are no longer interested: {0}", vote);
        }

        // process appendentries
        internal override void ProcessAppendEntries(AppendEntries<T> appendEntries)
        {
            if (appendEntries.LeaderTerm >= this.CurrentTerm)
            {
                this.Logger.InfoFormat(
                    "Received AppendEntries from a probable leader, stepping down.");

                this.Node.SwitchToAndProcessMessage(NodeStatus.Follower, appendEntries);
                return;
            }

            this.Logger.DebugFormat(
                "Received AppendEntries from an invalid leader, refusing ({0}).", appendEntries);
            AppendEntriesAck reply = new AppendEntriesAck(this.Node.Id, this.CurrentTerm, false);
            this.Node.SendMessage(appendEntries.LeaderId, reply);
        }

        // processes ack for appenentries
        // keeps track of other nodes
        // update commit index
        internal override void ProcessAppendEntriesAck(AppendEntriesAck appendEntriesAck)
        {
            this.Logger.TraceFormat("Received AppendEntriesAck ({0}).", appendEntriesAck);

            if (appendEntriesAck.Term > this.CurrentTerm)
            {
                this.Logger.DebugFormat("Term is higher, I resign.");
                this.Node.SwitchTo(NodeStatus.Follower);
                return;
            }

            string followerId = appendEntriesAck.NodeId;
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

            message.LeaderId = this.Node.Id;
            message.LeaderTerm = this.CurrentTerm;
            message.CommitIndex = this.Node.LastCommit;
            this.Node.SendMessage(followerId, message);
        }

        protected override void HeartbeatTimeouted()
        {
            // send keep alive
            this.BroadcastHeartbeat();
        }

        // compute the new commit index
        private void UpdateCommitIndex()
        {
            var ordered = this.states.Values.Select(state => state.MinSynchronizedIndex).OrderBy(value => value);
            int index = this.Node.Settings.Nodes.Length - this.Node.Settings.Majority + 1;
            int commitIndex = ordered.ElementAt(index);
            this.Node.Commit(commitIndex);
        }

        private void BroadcastHeartbeat()
        {
            foreach (var entry in this.states)
            {
                var followerLogState = entry.Value;
                var message = followerLogState.GetAppendEntries(this.Node.State.LogEntries);
                if (message != null)
                {
                    message.CommitIndex = this.Node.LastCommit;
                    message.LeaderId = this.Node.Id;
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

            private readonly ILog logger;

            private int minSynchronizedIndex;

            private int lastSentIndex = -1;

            private bool flyingTransaction;

            private DateTime lastSentMessageTime;

            #endregion

            /// <summary>
            /// Initializes a new instance of the <see cref="LogReplicationAgent"/> class.
            /// </summary>
            /// <param name="maxDelay">
            /// The max delay between two messages.
            /// </param>
            /// <param name="logSize">
            /// Size of the log.
            /// </param>
            /// <param name="nodeId">Node id, used for log purposes.
            /// </param>
            /// <param name="master">Master logger to capture name.
            /// </param>
            public LogReplicationAgent(TimeSpan maxDelay, int logSize, string nodeId, ILoggerWrapper master)
            {
                this.maxDelay = maxDelay;
                this.minSynchronizedIndex = logSize - 1;
                this.lastSentMessageTime = DateTime.MinValue;
                this.logger = LogManager.GetLogger(
                        String.Format("{0}(Replicator for {1})", master.Logger.Name, nodeId));
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

                int entriesToSend = Math.Max(
                    0, Math.Min(MaxBatch, log.Count - this.minSynchronizedIndex - 1));

                if (entriesToSend == 0 && !this.DelayElapsed)
                {
                    return null;
                }

                this.flyingTransaction = true;
                var message = new AppendEntries<T>
                                  {
                                      PrevLogIndex = this.minSynchronizedIndex,
                                      PrevLogTerm =
                                          this.minSynchronizedIndex < 0
                                              ? -1
                                              : log[this.minSynchronizedIndex].Term,
                                      Entries = new LogEntry<T>[entriesToSend]
                                  };
                int offset = this.minSynchronizedIndex + 1;
                
                this.logger.TraceFormat(
                    "Replicating {0} entries starting at {1}.", entriesToSend, offset);

                for (int i = 0; i < entriesToSend; i++)
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

                // it fails, log is not synchronised, so we will try on step earlier
                this.minSynchronizedIndex--;
            }
        }
    }
}
