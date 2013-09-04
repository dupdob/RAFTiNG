// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BasicNodeTest.cs" company="Cyrille DUPUYDAUBY">
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
    using System.Threading;

    using NFluent;

    using NUnit.Framework;

    using RAFTiNG.Messages;

    [TestFixture]
    public class BasicNodeTest
    {
        private object lastMessage;
        private readonly object synchro = new object();

        private PersistedState<string> BuildLog(int entries)
        {
            var test = new PersistedState<string>();

            for (var i = 0; i < entries; i++)
            {
                test.AddEntry(new LogEntry<string>("dummy"));
            }
            return test;
        }

       [Test]
        public void DefaultNodeStateIsOk()
        {
            var settings = Helpers.BuildNodeSettings("1", new string[] { "2", "3", "4" });
            var node = new Node<string>(settings);

            Check.ThatEnum(node.Status).IsEqualTo(NodeStatus.Initializing);

            Check.That(node.State.CurrentTerm).IsEqualTo(0L);

            Check.That(node.State.VotedFor).IsNullOrEmpty();

            Check.That(node.State.LogEntries).IsEmpty();
        }

        [Test]
        public void NodeInitStartsTheAgent()
        {
            var settings = Helpers.BuildNodeSettings("1", new string[] { "1", "2", "3", "4", "5" });
            settings.TimeoutInMs = 1;

            var node = new Node<string>(settings);

            node.SetMiddleware(new Middleware());
            node.Initialize();

            Check.ThatEnum(node.Status).IsEqualTo(NodeStatus.Follower);
        }

        [Test]
        public void NodeSwitchesToCandidate()
        {
            var settings = Helpers.BuildNodeSettings("1", new string[] { "1", "2", "3", "4", "5" });
            settings.TimeoutInMs = 1;

            var node = new Node<string>(settings);

            node.SetMiddleware(new Middleware());
            node.Initialize();
            Thread.Sleep(30);
            
            // should switch to candidate
            Check.ThatEnum(node.Status).IsEqualTo(NodeStatus.Candidate);
        }

        [Test]
        public void GrantVoteTest()
        {
            var middleware = new Middleware();
            var settings = Helpers.BuildNodeSettings("1", new string[] { "1", "2", "3", "4", "5" });
            settings.TimeoutInMs = 100;
            var node = new Node<string>(settings);

            node.SetMiddleware(middleware);
            node.Initialize();

            middleware.RegisterEndPoint("2", MessageReceived);

            GrantVote answer;
            lock (this.synchro)
            {
                // request a vote, and lie about our capacity
                middleware.SendMessage("1", new RequestVote(2, "2", 2, 2));

                if (this.lastMessage == null)
                {
                    Monitor.Wait(this.synchro, 50);
                }
                Check.That(this.lastMessage).IsNotEqualTo(null).And.IsInstanceOf<GrantVote>();

                answer = this.lastMessage as GrantVote;
            }

            // did we get the vote?
            Check.That(answer.VoteGranted).IsTrue();
            Check.That(node.State.VotedFor).IsEqualTo("2");

            // now, add entries
            node.AddEntry("dummy");
        }

        private void MessageReceived(object obj)
        {
            lock (this.synchro)
            {
                this.lastMessage = obj;
                Monitor.Pulse(this.synchro);
            }
        }
    }
}
