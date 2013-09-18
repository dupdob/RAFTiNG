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

    using log4net;

    using RAFTiNG.Messages;
    using RAFTiNG.States;

    /// <summary>
    /// Implements a Node as described by the RAFT algorithm
    /// </summary>
    /// <typeparam name="T">Command type for the internal state machine.</typeparam>
    public sealed class Node<T> : IDisposable
    {
        #region fields

        private readonly ILog logger;

        private IMiddleware middleware;

        private NodeSettings settings;

        private State<T> currentState;

        #endregion

        #region constructor

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
            this.logger = LogManager.GetLogger(string.Format("Node[{0}]", this.Address));
        }

        #endregion

        #region properties

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

        internal ILog Logger
        {
            get
            {
                return this.logger;
            }
        }

        internal int TimeOutInMs
        {
            get
            {
                return this.settings.TimeoutInMs;
            }
        }

        internal NodeSettings Settings
        {
            get
            {
                return this.settings;
            }
        }

        #endregion

        #region methods

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
            logger.Info("Stopping Node.");
            if (this.currentState != null)
            {
                this.currentState.ExitState();
            }
        }

        internal void SwitchTo(NodeStatus status)
        {
            State<T> newState;

            if (this.logger.IsDebugEnabled)
            {
                this.logger.DebugFormat("Switching status from {0} to {1}", this.Status, status);
            }

            if (this.currentState != null)
            {
                this.currentState.ExitState();
            }

            switch (status)
            {
                case NodeStatus.Follower:
                    newState = new Follower<T>(this);
                    break;
                case NodeStatus.Candidate:
                    newState = new Candidate<T>(this);
                    break;
                case NodeStatus.Leader:
                    newState = new Leader<T>(this);
                    break;
                default:
                    throw new NotSupportedException("Status not supported");
            }

            this.Status = status;
            this.currentState = newState;
            this.currentState.EnterState();
        }

        internal void SwitchToAndProcessMessage(NodeStatus status, object message)
        {
            this.SwitchTo(status);
            this.MessageReceived(message);
        }

        internal long IncrementTerm()
        {
            var nextTerm = this.State.CurrentTerm + 1;
            this.State.CurrentTerm = nextTerm;
            return nextTerm;
        }

        internal void SendMessage(string dest, object message)
        {
            this.middleware.SendMessage(dest, message);
        }

        internal void SendToAll(object message)
        {
            this.logger.TraceFormat("Broadcast message to all: {0}", message);

            // send request to all nodes
            foreach (var otherNode in this.settings.Nodes)
            {
                this.middleware.SendMessage(otherNode, message);
            }
        }

        internal void SendToOthers(object message)
        {
            this.logger.TraceFormat("Broadcast message to all other nodes: {0}", message);

            // send request to all nodes
            foreach (var otherNode in this.settings.OtherNodes())
            {
                this.middleware.SendMessage(otherNode, message);
            }
        }

        private void MessageReceived(object obj)
        {
            this.MessagesCount++;

            var requestVote = obj as RequestVote;
            if (requestVote != null)
            {
                this.currentState.ProcessVoteRequest(requestVote);
            }

            var vote = obj as GrantVote;
            if (vote != null)
            {
                this.currentState.ProcessVote(vote);
            }

            var appendEntries = obj as AppendEntries<T>;
            if (appendEntries != null)
            {
                this.currentState.ProcessAppendEntries(appendEntries);
            }
        }

        #endregion
    }
}
