//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogReplicationTest.cs" company="Cyrille DUPUYDAUBY">
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
    public class LogReplicationTest
    {
        [Test]
        public void EmptyLogIsfilledTest()
        {
            using (Helpers.InitLog4Net())
            {
                var settings = Helpers.BuildNodeSettings("1", new[] { "1", "2" });
                settings.TimeoutInMs = Timeout.Infinite;
                var logReplicator = new LogReplicator<string>();
                var middleware = new Middleware();

                using (var node = new Node<string>(settings))
                {
                    settings = Helpers.BuildNodeSettings("2", new[] { "1", "2" });
                    settings.TimeoutInMs = 1;
                    var leader = new Node<string>(settings);
                    leader.SetMiddleware(middleware);
                    node.SetMiddleware(middleware);
                    node.Initialize();
                    leader.Initialize();
                    Thread.Sleep(30);

                    // should stay follower
                    Check.ThatEnum(node.Status).IsEqualTo(NodeStatus.Follower);

                    Check.ThatEnum(leader.Status).IsEqualTo(NodeStatus.Leader);
                }
            }
        }
    }

    public class LogReplicator<T>
    {
    }
}
