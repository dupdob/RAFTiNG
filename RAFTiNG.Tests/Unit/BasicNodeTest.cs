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
//   Implements various tests validating the basic of noce behavior
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace RAFTiNG.Tests.Unit
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    using Michonne.Implementation;

    using NFluent;

    using NUnit.Framework;

    using RAFTiNG.Messages;
    using RAFTiNG.Tests.Services;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", 
        Justification = "Reviewed. Suppression is OK here.")]
    [TestFixture]
    public class BasicNodeTest
    {
        #region Fields

        private readonly object synchro = new object();

        private object lastMessage;

        #endregion

        #region Public Methods and Operators

        [Test]
        public void DefaultNodeStateIsOk()
        {
            var settings = Helpers.BuildNodeSettings("1", new[] { "2", "3", "4" });
            using (var node = new Node<string>(TestHelpers.GetPool().BuildSequencer(), settings, new Middleware(), new StateMachine()))
            {
                Check.ThatEnum(node.Status).IsEqualTo(NodeStatus.Initializing);

                Check.That(node.State.CurrentTerm).IsEqualTo(0L);

                Check.That(node.State.VotedFor).IsNullOrEmpty();

                Check.That(node.State.LogEntries).IsEmpty();
            }
        }

        [Test]
        public void NodeInitStartsTheAgent()
        {
            var settings = Helpers.BuildNodeSettings("1", new[] { "1", "2", "3", "4", "5" });
            settings.TimeoutInMs = 10;

            using (var node = new Node<string>(TestHelpers.GetPool().BuildSequencer(), settings, new Middleware(), new StateMachine()))
            {
                node.Initialize();

                Check.ThatEnum(node.Status).IsEqualTo(NodeStatus.Follower);
            }
        }

        [Test]
        public void NodeStaysAFollowerWhenReceiveAppendEntries()
        {
            using (Helpers.InitLog4Net())
            {
                var settings = Helpers.BuildNodeSettings("1", new[] { "1", "2", "3", "4", "5" });
                settings.TimeoutInMs = 20;
                var middleware = new Middleware();
                var node = new Node<string>(TestHelpers.GetPool().BuildSequencer(), settings, middleware, new StateMachine());

                using (node)
                {
                    node.Initialize();

                    // should switch to candidate
                    Check.That(this.WaitState(node, NodeStatus.Candidate, 40)).IsTrue();

                    // now we pretend there is a leader
                    var message = new AppendEntries<string>
                                      {
                                          LeaderId = "2", 
                                          LeaderTerm = 5, 
                                          PrevLogIndex = -1, 
                                          PrevLogTerm = 0
                                      };

                    var entry = new LogEntry<string>("dummy", 1L);
                    message.Entries = new[] { entry };
                    middleware.SendMessage("1", message);
                    Check.That(this.WaitState(node, NodeStatus.Follower, 30)).IsTrue();

                    Check.That(node.State.LogEntries.Count).IsEqualTo(1);
                }
            }
        }

        [Test]
        public void NodeSwitchesToCandidate()
        {
            var settings = Helpers.BuildNodeSettings("1", new[] { "1", "2", "3", "4", "5" });
            settings.TimeoutInMs = 10;

            using (var node = new Node<string>(TestHelpers.GetPool().BuildSequencer(), settings, new Middleware(), new StateMachine()))
            {
                node.Initialize();
                Thread.Sleep(40);

                // should switch to candidate
                Check.ThatEnum(node.Status).IsEqualTo(NodeStatus.Candidate);
            }
        }

        [Test]
        public void NodeWithLongerLogOlderTermGrantsVote()
        {
            Node<string> node;
            var middleware = this.InitNodes(out node);
            using (node)
            {
                // now, add entries
                node.AddEntry("dummy");

                node.State.CurrentTerm = 1;
                node.AddEntry("dummy");
                node.AddEntry("dummy");
                node.AddEntry("dummy");
                this.RequestAndGetVote(middleware, node, true);
            }
        }

        [Test]
        public void NodeWithLongerLogSameTermDoesNotGrantVote()
        {
            Node<string> node;
            var middleware = this.InitNodes(out node);

            using (node)
            {
                // now, add entries
                node.AddEntry("dummy");

                node.State.CurrentTerm = 2;
                node.AddEntry("dummy");
                node.AddEntry("dummy");
                node.AddEntry("dummy");
                this.RequestAndGetVote(middleware, node, false);
            }
        }

        [Test]
        public void NodeWithMoreRescentTermDoesNotGrantVote()
        {
            Node<string> node;
            var middleware = this.InitNodes(out node);

            using (node)
            {
                // now, add entries
                node.AddEntry("dummy");

                node.State.CurrentTerm = 4;

                this.RequestAndGetVote(middleware, node, false);
            }
        }

        [Test]
        public void NodeWithNoLogsGrantsVote()
        {
            Node<string> node;
            var middleware = this.InitNodes(out node);

            using (node)
            {
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
                    Check.That(node.State.VotedFor).IsEqualTo("2");
                }

                Check.That(answer).IsNotNull();
                // did we get the vote?
                Check.That(answer.VoteGranted).IsTrue();
            }
        }

        [Test]
        public void NodeWithSameLogGrantsVote()
        {
            Node<string> node;
            var middleware = this.InitNodes(out node);

            using (node)
            {
                // now, add entries
                node.AddEntry("dummy");

                node.State.CurrentTerm = 2;
                node.AddEntry("dummy");
                node.AddEntry("dummy");
                this.RequestAndGetVote(middleware, node, true);
            }
        }

        #endregion

        #region Methods

        private PersistedState<string> BuildLog(int entries)
        {
            var test = new PersistedState<string>();

            for (var i = 0; i < entries; i++)
            {
                test.AddEntry(new LogEntry<string>("dummy"));
            }

            return test;
        }

        // helper to initilize setip
        private Middleware InitNodes(out Node<string> node)
        {
            var middleware = new Middleware(false);
            var settings = Helpers.BuildNodeSettings("1", new[] { "1", "2", "3", "4", "5" });
            settings.TimeoutInMs = Timeout.Infinite; // no timeout
            node = new Node<string>(middleware.RootUnitOfExecution.BuildSequencer(), settings, middleware, new StateMachine());

            node.Initialize();

            middleware.RegisterEndPoint("2", this.MessageReceived);
            return middleware;
        }

        private void MessageReceived(object obj)
        {
            lock (this.synchro)
            {
                this.lastMessage = obj;
                Monitor.Pulse(this.synchro);
            }
        }

        private void RequestAndGetVote(Middleware middleware, Node<string> node, bool succeed)
        {
            lock (this.synchro)
            {
                // request a vote, and lie about our capacity
                middleware.SendMessage("1", new RequestVote(3, "2", 2, 2));

                if (this.lastMessage == null)
                {
                    Monitor.Wait(this.synchro, 100);
                }

                Check.That(this.lastMessage).IsNotEqualTo(null).And.IsInstanceOf<GrantVote>();

                var answer = this.lastMessage as GrantVote;
                if (succeed)
                {
                    Check.That(node.State.VotedFor).IsEqualTo("2");

                    // did we get the vote?
                    Check.That(answer.VoteGranted).IsTrue();
                }
                else
                {
                    Check.That(answer.VoteGranted).IsFalse();
                }
            }
        }

        private bool WaitState(Node<string> node, NodeStatus status, int waitTime)
        {
            int step = 10;
            for (int delay = 0; delay < waitTime; delay += step)
            {
                if (node.Status == status)
                {
                    return true;
                }

                Thread.Sleep(step);
            }

            return node.Status == status;
        }

        #endregion
    }
}