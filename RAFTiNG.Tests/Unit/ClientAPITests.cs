//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientAPITests.cs" company="Cyrille DUPUYDAUBY">
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

    using RAFTiNG.Commands;

    /// <summary>
    /// 
    /// </summary>
    [TestFixture]
    public class ClientAPITests
    {
        [Test]
        public void ProtocolTest()
        {
            var settings = Helpers.BuildNodeSettings("1", new[] { "1" });
            settings.TimeoutInMs = 5;
            var raftMiddleware = new Middleware();
            var node = new Node<string>(settings, raftMiddleware);
            node.Initialize();
            Thread.Sleep(50);

            Check.ThatEnum(node.Status).IsEqualTo(NodeStatus.Leader);

            raftMiddleware.SendMessage("1", new SendCommand<string>("test"));
        }
    }
}
