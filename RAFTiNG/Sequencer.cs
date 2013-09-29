// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Sequencer.cs" company="Cyrille DUPUYDAUBY">
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
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Ensure all tasks are executed sequentially.
    /// </summary>
    public class Sequencer
    {
        private readonly Queue<Action> pending = new Queue<Action>();

        private bool acting;

        /// <summary>
        /// Sequences the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void Sequence(Action action)
        {
            // executa action
            lock (this.pending)
            {
                if (this.acting)
                {
                    this.pending.Enqueue(action);
                    return;
                }

                this.acting = true;
            }

            for (;;)
            {
                action();
                lock (this.pending)
                {
                    if (this.pending.Count == 0)
                    {
                        this.acting = false;
                        return;
                    }

                    action = this.pending.Dequeue();
                }
            }
        }
    }
}