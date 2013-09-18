//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="State.cs" company="Cyrille DUPUYDAUBY">
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
    using System.Threading;

    using log4net;

    using RAFTiNG.Messages;

    internal abstract class State<T>
    {
        protected readonly Node<T> Node;

        private static Random seed = new Random();

        private bool done;

        /// <summary>
        /// Initializes a new instance of the <see cref="State{T}"/> class.
        /// </summary>
        /// <param name="node">The node.</param>
        protected State(Node<T> node)
        {
            this.Node = node;
        }

        protected bool Done
        {
            get
            {
                return this.done;
            }
        }

        protected ILog Logger
        {
            get
            {
                return this.Node.Logger;
            }
        }

        protected long CurrentTerm
        {
            get
            {
                return this.Node.State.CurrentTerm;
            }
        }

        private Timer HeartBeatTimer { get; set; }

        internal abstract void EnterState();

        internal abstract void ProcessVoteRequest(RequestVote request);

        internal abstract void ProcessVote(GrantVote vote);

        internal abstract void ProcessAppendEntries(AppendEntries<T> appendEntries);

        internal void ExitState()
        {
            if (this.HeartBeatTimer != null)
            {
                this.HeartBeatTimer.Dispose();
            }

            this.done = true;
        }

        protected void ResetTimeout(double randomPart = 0.0, double fixPart = 1.0)
        {
            if (this.HeartBeatTimer != null)
            {
                this.HeartBeatTimer.Dispose();                
            }

            int timeout;
            if (this.Node.TimeOutInMs != Timeout.Infinite)
            {
                timeout =
                    (int)
                    (((seed.NextDouble() * randomPart) + (fixPart - randomPart))
                     * this.Node.TimeOutInMs);
                this.Logger.DebugFormat("Set timeout to {0} ms.", timeout);
            }
            else
            {
                timeout = this.Node.TimeOutInMs;
                this.Logger.Debug("Set timeout to infinite.");
            }

            this.HeartBeatTimer = new Timer(
                this.HeartbeatTimeouted, null, timeout, Timeout.Infinite);
        }

        protected abstract void HeartbeatTimeouted(object state);
    }
}