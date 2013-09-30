//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="SequencerTest.cs" company="Cyrille DUPUYDAUBY">
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

namespace RAFTiNG.Tests.Unit
{
    using System.Threading;

    using NFluent;

    using NUnit.Framework;

    [TestFixture]
    public class SequencerTest
    {
        [Test]
        public void SequenceTest()
        {
            var synchro = new object();
            var sequencer = new Sequencer();
            var failed = false;

            for (var i = 0; i < 10000; i++)
            {
                ThreadPool.QueueUserWorkItem(
                    (_) => sequencer.Sequence(
                        () =>
                            {
                                if (!Monitor.TryEnter(synchro))
                                {
                                    failed = true;
                                }
                                lock (synchro)
                                {
                                    Thread.Sleep(30);
                                }
                            }));
            }

            Check.That(failed).IsFalse();
        }
    }
}
