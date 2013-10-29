//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="IStateMachine.cs" company="Cyrille DUPUYDAUBY">
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
namespace RAFTiNG.Services
{
    /// <summary>
    /// Interface implemented by the business state machine that is hosted inside Rafting
    /// </summary>
    /// <typeparam name="T">Type of message/command supported by the sate machine.</typeparam>
    public interface IStateMachine<T>
    {
        /// <summary>
        /// Commits the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        void Commit(T command);
    }
}