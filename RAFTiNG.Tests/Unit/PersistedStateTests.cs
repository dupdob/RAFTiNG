// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PersistedStateTests.cs" company="Cyrille DUPUYDAUBY">
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

namespace RAFTiNG.Tests.Unit
{
    using NFluent;

    using NUnit.Framework;

    [TestFixture]
    public class PersistedStateTests
    {
        [Test]
        public void TermChangeResetsVotedFor()
        {
            var test = new PersistedState<string>();
            test.CurrentTerm = 2;
            test.VotedFor = "myself";

            test.CurrentTerm = 3;

            Check.That(test.CurrentTerm).IsEqualTo(3L);

            Check.That(test.VotedFor).IsNullOrEmpty();
        }

        [Test]
        public void CanAddCommands()
        {
            var test = new PersistedState<string>();

            test.AddEntry(new LogEntry<string>("dummy"));
            test.AddEntry(new LogEntry<string>("dummy"));

            Check.That(test.LogEntries[0].Index).IsEqualTo(0L);
            Check.That(test.LogEntries[1].Term).IsEqualTo(0L);
            Check.That(test.LogEntries[1].Index).IsEqualTo(1L);
        }
    }
}
