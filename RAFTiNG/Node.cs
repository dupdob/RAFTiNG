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
    /// <summary>
    /// Implements a Node as described by the RAFT algorithm
    /// </summary>
    ///<typeparam name="T">Command type for the internal state machine.</typeparam>
    public class Node<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node{T}"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        public Node(string address)
        {
            this.Address = address;
            this.Status = NodeStatus.Initializing;
            this.State = new PersistedState<T>();
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
            test.RegisterEndPoint(this.Address, this.MessageReceived);
        }

        private void MessageReceived(object obj)
        {
            this.MessagesCount++;
        }
    }
}