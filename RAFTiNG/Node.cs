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

    using RAFTiNG.Commands;
    using RAFTiNG.Messages;
    using RAFTiNG.States;

    /// <summary>
    /// Implements a Node as described by the RAFT algorithm.
    /// The nodes aggregates a persisted state object that must be kept synchronized with a persistent storage.
    /// The node also aggregates a <see cref="State"/> subclass instance to implement the active state rules.
    /// </summary>
    /// <typeparam name="T">Command type for the middleware state machine.</typeparam>
    public sealed class Node<T> : IDisposable
    {
        #region fields

        private readonly Sequencer sequencer = new Sequencer();
        
        private readonly ILog logger;

        private readonly IMiddleware internalMiddleware;

        private NodeSettings settings;

        private State<T> currentState;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Node{T}"/> class.
        /// </summary>
        /// <param name="settings">
        /// The node settings.
        /// </param>
        /// <param name="middleware">Middleware used to exchange message.
        /// </param>
        public Node(NodeSettings settings, IMiddleware middleware)
        {
            this.Id = settings.NodeId;
            this.settings = settings;
            this.internalMiddleware = middleware;

            this.Status = NodeStatus.Initializing;
            this.State = new PersistedState<T>();
            this.logger = LogManager.GetLogger(string.Format("Node[{0}]", this.Id));
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
        public string Id { get; private set; }

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

        /// <summary>
        /// Gets the timeout base value.
        /// </summary>
        internal int TimeOutInMs
        {
            get
            {
                return this.settings.TimeoutInMs;
            }
        }

        /// <summary>
        /// Gets the RAFTiNG settings.
        /// </summary>
        internal NodeSettings Settings
        {
            get
            {
                return this.settings;
            }
        }

        /// <summary>
        /// Gets or sets the leader id.
        /// </summary>
        /// <value>
        /// The leader id.
        /// </value>
        internal string LeaderId { get; set; }
        
        #endregion

        #region methods

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (this.internalMiddleware == null)
            {
                throw new InvalidOperationException("Must call SetInternalMiddleware first!");
            }

            // TODO: restore persisted state
            if (this.Status != NodeStatus.Initializing)
            {
                throw new InvalidOperationException("Node is already initialized.");
            }

            this.logger.InfoFormat("Middleware registration to address {0}.", this.Id);
            this.internalMiddleware.RegisterEndPoint(this.Id, this.MessageReceived);
            this.SwitchTo(NodeStatus.Follower);
        }

        /// <summary>
        /// Adds an entry to this node.
        /// </summary>
        /// <param name="command">The command.</param>
        public void AddEntry(T command)
        {
            this.sequencer.Sequence(() => this.State.AddEntry(command));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.logger.Info("Stopping Node.");
            if (this.currentState != null)
            {
                this.currentState.ExitState();
                this.currentState = null;
            }
        }

        /// <summary>
        /// Sends the command into the cluster.
        /// </summary>
        /// <param name="command">The command to be processed.</param>
        /// <returns>true if the command was send, which means the cluster has a leader.</returns>
        public bool SendCommand(T command)
        {
            if (string.IsNullOrEmpty(this.LeaderId))
            {
                // no leader
                return false;
            }

            this.Sequence(
                () =>
                {
                    if (string.IsNullOrEmpty(this.LeaderId))
                    {
                        // no leader
                        return;
                    }

                    var message = new SendCommand<T>(command);
                    if (this.LeaderId == this.Id)
                    {
                        this.SequencedMessageReceived(message);
                    }
                    else
                    {
                        this.SendMessage(this.LeaderId, message);
                    }
                });
            return true;
        }

        #region Methodes used by the state object

        /// <summary>
        /// Changes the current status.
        /// </summary>
        /// <param name="status">Target status.</param>
        internal void SwitchTo(NodeStatus status)
        {
            this.SequencedSwitch(status);
        }

        /// <summary>
        /// Changes the current status and process the given message.
        /// </summary>
        /// <param name="status">Target status.</param>
        /// <param name="message">Message to be processed.</param>
        /// <remarks>Used this method when a message triggers a step down.</remarks>
        internal void SwitchToAndProcessMessage(NodeStatus status, object message)
        {
            this.SequencedSwitch(status);
            this.SequencedMessageReceived(message);
        }

        /// <summary>
        /// Increase the current term.
        /// </summary>
        /// <returns>The new term.</returns>
        internal long IncrementTerm()
        {
            var nextTerm = this.State.CurrentTerm + 1;
            this.State.CurrentTerm = nextTerm;
            return nextTerm;
        }

        /// <summary>
        /// Send message to a specific node. 
        /// </summary>
        /// <param name="dest">Destination node.</param>
        /// <param name="message">Message to be sent.</param>
        internal void SendMessage(string dest, object message)
        {
            this.internalMiddleware.SendMessage(dest, message);
        }

        /// <summary>
        /// Broadcast a message to all other nodes.
        /// </summary>
        /// <param name="message">Message to be broadcasted.</param>
        internal void SendToOthers(object message)
        {
            this.logger.TraceFormat("Broadcast message to all other nodes: {0}", message);

            // send request to all nodes
            foreach (var otherNode in this.settings.OtherNodes())
            {
                this.internalMiddleware.SendMessage(otherNode, message);
            }
        }

        /// <summary>
        /// Ensure an <see cref="System.Action"/> is executed non concurrently.
        /// </summary>
        /// <param name="action">Action to sequence.</param>
        /// <remarks>Use this function to prevent race conditions, so it should be the entry point for all messages, timers and other asynchronous calls.</remarks>
        internal void Sequence(Action action)
        {
            this.sequencer.Sequence(action);
        }

        /// <summary>
        /// Implementation of the Switch method, assuming no race conditions is possible.
        /// </summary>
        /// <param name="status">Target status.</param>
        private void SequencedSwitch(NodeStatus status)
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

        #endregion

        // raft message received
        private void MessageReceived(object obj)
        {
            this.sequencer.Sequence(() => this.SequencedMessageReceived(obj));
        }

        /// <summary>
        /// Process a received message. Basically route the message to the adequate handler.
        /// </summary>
        /// <param name="obj">Message to route.</param>
        private void SequencedMessageReceived(object obj)
        {
            if (this.currentState == null)
            {
                this.Logger.DebugFormat("Node is not active, discarding message ({0}).", obj);
                return;
            }

            this.MessagesCount++;

            var requestVote = obj as RequestVote;
            if (requestVote != null)
            {
                this.currentState.ProcessVoteRequest(requestVote);
                return;
            }

            var vote = obj as GrantVote;
            if (vote != null)
            {
                this.currentState.ProcessVote(vote);
                return;
            }

            var appendEntries = obj as AppendEntries<T>;
            if (appendEntries != null)
            {
                this.currentState.ProcessAppendEntries(appendEntries);
                return;
            }

            var appendEntriesAck = obj as AppendEntriesAck;
            if (appendEntriesAck != null)
            {
                this.currentState.ProcessAppendEntriesAck(appendEntriesAck);
                return;
            }

            this.logger.ErrorFormat("Message not recognized (Type: {0}, Message: {1})", obj.GetType(), obj);
        }

        #endregion
    }
}
