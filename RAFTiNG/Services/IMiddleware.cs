// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMiddleware.cs" company="Cyrille DUPUYDAUBY">
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

namespace RAFTiNG.Services
{
    using System;

    /// <summary>
    /// Messaging middleware
    /// </summary>
    public interface IMiddleware
    {
        /// <summary>
        /// Sends a message to a specific address.
        /// </summary>
        /// <param name="addressDest">The address to send the message to.</param>
        /// <param name="message">The message to be sent.</param>
        /// <returns>false if the message was not sent.</returns>
        /// <remarks>This is a best effort delivery contract. There is no guaranteed delivery.</remarks>
        bool SendMessage(string addressDest, object message);

        /// <summary>
        /// Registers the end point to process received messages.
        /// </summary>
        /// <param name="address">The address to register to.</param>
        /// <param name="messageReceived">The message processing method.</param>
        /// <exception cref="System.InvalidOperationException">If an endpoint is registered more than once.</exception>
        void RegisterEndPoint(string address, Action<object> messageReceived);
    }
}