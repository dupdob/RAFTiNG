// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Node.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the Node type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RAFTiNG
{
    using System;

    /// <summary>
    /// Implements a Node as described by the RAFT algorithm
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        public Node(string address)
        {
            this.Address = address;
        }

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

    }
}