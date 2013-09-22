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

            Check.That(test.LogEntries[0].Term).IsEqualTo(0L);
            Check.That(test.LogEntries[1].Term).IsEqualTo(0L);
        }

        [Test]
        public void LastPersistedInfoWorks()
        {
            var test = new PersistedState<string> { CurrentTerm = 1 };
            test.AddEntry(new LogEntry<string>("dummy"));
            Check.That(test.LastPersistedTerm).IsEqualTo(1L);
            Check.That(test.LastPersistedIndex).IsEqualTo(0);
        }

        [Test]
        public void LogIsBetterWorks()
        {
            var test = new PersistedState<string> { CurrentTerm = 1 };
            Check.That(test.LogIsBetterThan(0, 0)).IsFalse();
            test.AddEntry(new LogEntry<string>("dummy"));
            Check.That(test.LogIsBetterThan(0, 0)).IsTrue();
            Check.That(test.LogIsBetterThan(1, 0)).IsFalse();
        }

        [Test]
        public void LogEntryMatches()
        {
            var test = new PersistedState<string> { CurrentTerm = 1 };

            test.AddEntry(new LogEntry<string>("dummy"));
            test.CurrentTerm = 2;
            test.AddEntry(new LogEntry<string>("dummy"));
            Check.That(test.EntryMatches(0, 1L)).IsTrue();
        }

        [Test]
        public void AppendEntries()
        {
            var test = new PersistedState<string> { CurrentTerm = 1 };

            test.AddEntry(new LogEntry<string>("dummy"));
            test.CurrentTerm = 2;
            test.AppendEntries(-1,  new[] {new LogEntry<string>("dummy", 2)});
            Check.That(test.EntryMatches(0, 2L)).IsTrue();
        }
    }
}
