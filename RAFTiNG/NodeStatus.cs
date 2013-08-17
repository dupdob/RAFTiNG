// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NodeStatus.cs" company="">
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
    /// <summary>
    /// List the various states of a Node
    /// </summary>
    public enum NodeStatus
    {
        /// <summary>
        /// Node is booting up
        /// </summary>
        Initializing,

        /// <summary>
        /// The node is passive, acting as a replicator for the leader
        /// </summary>
        Follower,
        
        /// <summary>
        /// The node is looking for a majority to be elected leader
        /// </summary>
        Candidate,

        /// <summary>
        /// This is the active node, in charge of making progress
        /// </summary>
        Leader
    }
}