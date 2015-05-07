﻿//  --------------------------------------------------------------------------------------------------------------------
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

    /// <summary>
    /// Base class for the various node's states. Implement some default behavior.
    /// </summary>
    /// <typeparam name="T">Type for automaton commands.</typeparam>
    internal abstract class State<T>
    {
        #region fields

        protected readonly Node<T> Node;

        private static readonly Random Seed = new Random();

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="State{T}"/> class.
        /// </summary>
        /// <param name="node">The node.</param>
        protected State(Node<T> node)
        {
            this.Node = node;
        }

        #endregion

        #region properties

        protected bool Done { get; private set; }

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

        private NodeSettings Settings
        {
            get
            {
                return this.Node.Settings;
            }
        }

        private Timer HeartBeatTimer { get; set; }

        #endregion

        #region methods

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

            this.Done = true;
        }

        internal virtual void ProcessAppendEntriesAck(AppendEntriesAck appendEntriesAck)
        {
            this.Logger.WarnFormat(
                "Received ProcessAppendEntriesAck but I am not a leader, discarded: {0}",
                appendEntriesAck);
        }

        protected void ResetTimeout(double randomPart = 0.0, double fixPart = 1.0)
        {
            if (this.Done)
            {
                return;
            }

            if (this.HeartBeatTimer != null)
            {
                this.HeartBeatTimer.Dispose();
            }

            int timeout;
            if (this.Settings.TimeoutInMs != Timeout.Infinite)
            {
                timeout =
                    (int)
                    (((Seed.NextDouble() * randomPart * 2.0) + (fixPart - randomPart))
                     * this.Settings.TimeoutInMs);
                if (timeout < 10)
                {
                    timeout = 10;
                }

                this.Logger.DebugFormat("Set timeout to {0} ms.", timeout);
            }
            else
            {
                timeout = this.Settings.TimeoutInMs;
                if (this.Logger.IsDebugEnabled)
                {
                    this.Logger.Debug("Set timeout to infinite.");
                }
            }

            this.HeartBeatTimer = new Timer(
                this.InternalTimerHandler, null, timeout, Timeout.Infinite);
        }

        protected abstract void HeartbeatTimeouted();

        private void InternalTimerHandler(object state)
        {
            this.Node.Sequence(this.HeartbeatTimeouted);
        }

        #endregion
    }
}