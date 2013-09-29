namespace RAFTiNG.Tests.Unit
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