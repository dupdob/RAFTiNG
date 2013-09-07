// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Node.cs" company="Cyrille DUPUYDAUBY">
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
//   Defines the Node type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RAFTiNG
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using log4net;

    using RAFTiNG.Messages;

    /// <summary>
    /// Implements a Node as described by the RAFT algorithm
    /// </summary>
    /// <typeparam name="T">Command type for the internal state machine.</typeparam>
    public sealed class Node<T> : IDisposable
    {
        #region properties

        private readonly ILog logger = LogManager.GetLogger("Node");

        private Timer heartBeatTimer;

        private IMiddleware middleware;

        private IDictionary<string, GrantVote> voteReceived;

        private NodeSettings settings;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Node{T}"/> class.
        /// </summary>
        /// <param name="settings">The node settings.</param>
        public Node(NodeSettings settings)
        {
            this.Address = settings.NodeId;
            this.settings = settings;

            this.Status = NodeStatus.Initializing;
            this.State = new PersistedState<T>();
        }

        ~Node()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the current status for this node.
        /// </summary>
        public NodeStatus Status { get; private set; }

        /// <summary>
        /// Gets the node address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public string Address { get; private set; }

        /// <summary>
        /// Gets the messages count.
        /// </summary>
        /// <value>
        /// The messages count.
        /// </value>
        public int MessagesCount { get; private set; }

        /// <summary>
        /// Gets the persisted state.
        /// </summary>
        public PersistedState<T> State { get; private set; }

        /// <summary>
        /// Sets the middleware for the node
        /// </summary>
        /// <param name="test">The test.</param>
        public void SetMiddleware(IMiddleware test)
        {
            if (test == null)
            {
                throw new ArgumentNullException("test");
            }

            this.logger.InfoFormat("Middleware registration to address {0}", this.Address);
            test.RegisterEndPoint(this.Address, this.MessageReceived);
            this.middleware = test;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (this.middleware == null)
            {
                throw new InvalidOperationException("Must call SetMiddleware first!");
            }

            // TODO: restore persisted state
            if (this.Status != NodeStatus.Initializing)
            {
                throw new InvalidOperationException("Node is already initialized.");
            }

            this.SwitchTo(NodeStatus.Follower);
        }

        /// <summary>
        /// Adds a entry.
        /// </summary>
        /// <param name="command">The command.</param>
        public void AddEntry(T command)
        {
            this.State.AddEntry(command);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void SwitchTo(NodeStatus status)
        {
            if (this.logger.IsDebugEnabled)
            {
                this.logger.DebugFormat("Switching status from {0} to {1}", this.Status, status);
            }

            switch (status)
            {
                case NodeStatus.Follower:
                    this.ResetTimeout();
                    this.Status = NodeStatus.Follower;
                    break;
                case NodeStatus.Candidate:
                    this.Status = NodeStatus.Candidate;
                    this.voteReceived = new Dictionary<string, GrantVote>();
                    this.StopTimeout();
                    this.RequestVote();
                    break;
            }
        }

        private void RequestVote()
        {
            // increase term
            long nextTerm = this.State.CurrentTerm + 1;
            this.State.CurrentTerm = nextTerm;

            // vote for self
            this.State.VotedFor = this.Address;
            var request = new RequestVote(nextTerm, this.Address, 0, 0);

            // send request to others
            foreach (var otherNode in this.settings.OtherNodes)
            {
                this.middleware.SendMessage(otherNode, request);
            }
        }

        private void ResetTimeout()
        {
            this.StopTimeout();

            this.heartBeatTimer = new Timer(
                this.HeartbeatTimeouted, null, this.settings.TimeoutInMs, Timeout.Infinite);
        }

        private void StopTimeout()
        {
            if (this.heartBeatTimer != null)
            {
                var temp = this.heartBeatTimer;
                this.heartBeatTimer = null;
                temp.Dispose();
            }
        }

        private void HeartbeatTimeouted(object state)
        {
            this.logger.Warn("Timeout elapsed without sign from current leader.");

            this.logger.Info("Trigger an election.");

            // going to candidate
            this.SwitchTo(NodeStatus.Candidate);
        }

        private void MessageReceived(object obj)
        {
            this.MessagesCount++;

            var requestVote = obj as RequestVote;
            if (requestVote != null)
            {
                this.GrantVote(requestVote);
            }

            var vote = obj as GrantVote;
            if (vote != null)
            {
                this.ProcessVote(vote);
            }
        }

        /// <summary>
        /// Process a received vote.
        /// </summary>
        /// <param name="message">Received message.</param>
        private void ProcessVote(GrantVote message)
        {
            if (this.Status != NodeStatus.Candidate && this.Status != NodeStatus.Leader)
            {
                this.logger.WarnFormat(
                    "Received a vote but I am not a candidate or a leader now. Message discarded: {0}", message);
                return;
            }

            if (message.VoterTerm > this.State.CurrentTerm)
            {
                this.State.CurrentTerm = message.VoterTerm;
                this.SwitchTo(NodeStatus.Follower);
                this.logger.DebugFormat("Received a vote from a node with a higher term. Stepping down. Message discarded {0}", message);
                return;
            }

            if (this.voteReceived.ContainsKey(message.VoterId))
            {
                // we already recieved a vote from the voter!
                this.logger.WarnFormat(
                    "We received a second vote from {0}. Initial vote: {1}. Second vote: {2}",
                    message.VoterId,
                    this.voteReceived[message.VoterId], 
                    message);
                return;
            }

            this.voteReceived.Add(message.VoterId, message);

            // count votes
            var votes = this.voteReceived.Values.Count(grantVote => grantVote.VoteGranted);

            if (votes < this.settings.Majority)
            {
                return;
            }

            // we have a majority
            this.logger.DebugFormat("I have been elected as new leader.");
            this.SwitchTo(NodeStatus.Leader);
        }

        /// <summary>
        /// Process a vote request
        /// </summary>
        /// <param name="requestVote">Process a request for a vote.</param>
        private void GrantVote(RequestVote requestVote)
        {
            GrantVote response;
            if (requestVote.Term < this.State.CurrentTerm)
            {
                // requesting a vote for a node that has less recent information
                // we declinet
                response = new GrantVote(false, this.Address, this.State.CurrentTerm);
            }
            else
            {
                if (requestVote.Term > this.State.CurrentTerm)
                {
                    // we need to upgrade our term
                    this.State.CurrentTerm = requestVote.Term;
                    if (this.Status == NodeStatus.Candidate || this.Status == NodeStatus.Leader)
                    {
                        // we step down
                        this.SwitchTo(NodeStatus.Follower);
                    }
                }

                // we check how complete is the log ?
                if (this.State.LogIsBetterThan(requestVote.LastLogTerm, requestVote.LastLogIndex))
                {
                    // our log is better than the candidate's
                    response = new GrantVote(false, this.Address, this.State.CurrentTerm);
                }
                else if (string.IsNullOrEmpty(this.State.VotedFor)
                    || this.State.VotedFor == requestVote.CandidateId)
                {
                    this.State.VotedFor = requestVote.CandidateId;

                    // grant vote
                    response = new GrantVote(true, this.Address, this.State.CurrentTerm);
                }
                else
                {
                    // we already voted for someone
                    response = new GrantVote(false, this.Address, this.State.CurrentTerm);
                }
            }

            // send back the response
            this.middleware.SendMessage(requestVote.CandidateId, response);
        }

        private void Dispose(bool disposing)
        {
            if (this.heartBeatTimer != null)
            {
                this.heartBeatTimer.Dispose();
                this.heartBeatTimer = null;
            }
        }
    }
}
