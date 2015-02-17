// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Middleware.cs" company="Cyrille DUPUYDAUBY">
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

namespace RAFTiNG.Tests.Services
{
    using System;
    using System.Collections.Generic;

    using log4net;

    using Michonne.Implementation;
    using Michonne.Interfaces;

    using RAFTiNG.Services;

    /// <summary>
    /// Middleware simulates the message middleware used by rafting nodes to communicate and synchronize
    /// </summary>
    public class Middleware : IMiddleware
    {
        private readonly Dictionary<string, Action<object>> endpoints = new Dictionary<string, Action<object>>();

        private readonly Dictionary<string, ISequencer> sequencer = new Dictionary<string, ISequencer>(); 

        private readonly ILog logger = LogManager.GetLogger("MockMiddleware");

        private readonly IUnitOfExecution root;

        public IUnitOfExecution RootUnitOfExecution
        {
            get
            {
                return this.root;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Middleware"/> class.
        /// </summary>
        /// <param name="asyncMode">if set to <c>true</c> middleware is async mode.</param>
        public Middleware(bool asyncMode = true)
        {
            if (asyncMode)
            {
                this.root = TestHelpers.GetPool();
            }
            else
            {
                this.root = TestHelpers.GetSynchronousUnitOfExecution();
            }
        }

        /// <summary>
        /// Sends a message to a specific address.
        /// </summary>
        /// <param name="addressDest">The address to send the message to.</param>
        /// <param name="message">The message to be sent.</param>
        /// <returns>false if the message was not sent.</returns>
        /// <remarks>This is a best effort delivery contract. There is no guaranteed delivery.</remarks>
        public bool SendMessage(string addressDest, object message)
        {
            if (!this.endpoints.ContainsKey(addressDest))
            {
                return false;
            }
            try
            {
                this.sequencer[addressDest].Dispatch(
                    () => this.endpoints[addressDest].Invoke(message));
            }
            catch (Exception e)
            {
                // exceptions must not cross middleware boundaries
                this.logger.Error("Exception raised when processing message.", e);
            }
            return true;
        }

        /// <summary>
        /// Registers the end point to process received messages.
        /// </summary>
        /// <param name="address">The address to register to.</param>
        /// <param name="messageReceived">The message processing method.</param>
        /// <exception cref="System.InvalidOperationException">If an endpoint is registered more than once.</exception>
        public void RegisterEndPoint(string address, Action<object> messageReceived)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(address, "addressDest must contain a value.");
            }

            if (messageReceived == null)
            {
                throw new ArgumentNullException("messageReceived");
            }

            if (this.endpoints.ContainsKey(address))
            {
                // double registration is development error.
                throw new InvalidOperationException("Invalid registration attempt: endpoints can only be registered once.");
            }

            this.endpoints[address] = messageReceived;
            this.sequencer[address] = this.root.BuildSequencer();
        }
    }
}