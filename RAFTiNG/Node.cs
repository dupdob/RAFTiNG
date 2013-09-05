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
    using System.Threading;

    using RAFTiNG.Messages;

    /// <summary>
    /// Implements a Node as described by the RAFT algorithm
    /// </summary>
    /// <typeparam name="T">Command type for the internal state machine.</typeparam>
    public class Node<T> : IDisposable
    {
        #region properties

        private readonly string[] otherNodes;

        private readonly int timeoutInMs;

        private Timer heartBeatTimer;

        private Middleware middleware;

        private IList<string> voteReceived;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Node{T}"/> class.
        /// </summary>
        /// <param name="settings">The node settings.</param>
        public Node(NodeSettings settings)
        {
            this.Address = settings.NodeId;
            this.otherNodes = settings.OtherNodes ?? new string[] { };
            this.timeoutInMs = settings.TimeoutInMs;
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
        public void SetMiddleware(Middleware test)
        {
            if (test == null)
            {
                throw new ArgumentNullException("test");
            }
            test.RegisterEndPoint(this.Address, this.MessageReceived);
            this.middleware = test;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (middleware == null)
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
        /// <exception cref="System.NotImplementedException"></exception>
        public void AddEntry(T command)
        {
            this.State.AddEntry(command);
        }

        private void SwitchTo(NodeStatus status)
        {
            switch (status)
            {
                case NodeStatus.Follower:
                    this.ResetTimeout();
                    this.Status = NodeStatus.Follower;
                    break;
                case NodeStatus.Candidate:
                    this.Status = NodeStatus.Candidate;
                    this.voteReceived = new List<string>();
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
            foreach (var otherNode in this.otherNodes)
            {
                this.middleware.SendMessage(otherNode, request);
            }
        }

        private void ResetTimeout()
        {
            this.StopTimeout();

            this.heartBeatTimer = new Timer(
                this.HeartbeatTimeouted, null, this.timeoutInMs, Timeout.Infinite);
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
            // going to candidate
            this.SwitchTo(NodeStatus.Candidate);
        }

        private void MessageReceived(object obj)
        {
            this.MessagesCount++;

            if (obj is RequestVote)
            {
                this.GrantVote((RequestVote)obj);
            }
        }

        /// <summary>
        /// Process a vote request
        /// </summary>
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
                // can we grant the vote ?
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

        virtual protected void Dispose(bool disposing)
        {
            if (this.heartBeatTimer != null)
            {
                this.heartBeatTimer.Dispose();
                this.heartBeatTimer = null;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
