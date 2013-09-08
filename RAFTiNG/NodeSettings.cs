// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NodeSettings.cs" company="Cyrille DUPUYDAUBY">
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
//   
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RAFTiNG
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Stores all settings for a given node.
    /// </summary>
    public struct NodeSettings
    {
        /// <summary>
        /// Gets or sets the node id.
        /// </summary>
        /// <value>
        /// The node id.
        /// </value>
        public string NodeId { get; set; }

        /// <summary>
        /// Gets or sets the timeout in milliseconds.
        /// </summary>
        /// <value>
        /// The timeout value in milliseconds.
        /// </value>
        public int TimeoutInMs { get; set; }

        /// <summary>
        /// Gets or sets the list of nodes.
        /// </summary>
        /// <value>
        /// The other nodes.
        /// </value>
        public string[] Nodes { get; set; }

        /// <summary>
        /// Gets the list of other nodes.
        /// </summary>
        /// <returns>The list of nodes without this node.</returns>
        public IList<string> OtherNodes()
        {
            var tmpThis = this;
            return tmpThis.Nodes.Where(node => node != tmpThis.NodeId).ToList();
        }

        /// <summary>
        /// Gets the majority.
        /// </summary>
        /// <value>
        /// The majority.
        /// </value>
        public int Majority
        {
            get
            {
                return (((this.Nodes == null) ? 0 : this.Nodes.Length) + 3) / 2;
            }
        }
    }
}