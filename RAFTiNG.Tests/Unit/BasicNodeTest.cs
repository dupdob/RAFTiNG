// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="BasicNodeTest.cs" company="">
// //   Copyright 2013 Cyrille DUPUYDAUBY
// //   Licensed under the Apache License, Version 2.0 (the "License");
// //   you may not use this file except in compliance with the License.
// //   You may obtain a copy of the License at
// //       http://www.apache.org/licenses/LICENSE-2.0
// //   Unless required by applicable law or agreed to in writing, software
// //   distributed under the License is distributed on an "AS IS" BASIS,
// //   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //   See the License for the specific language governing permissions and
// //   limitations under the License.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------

namespace RAFTiNG.Tests.Unit
{
    using NFluent;

    using NUnit.Framework;

    [TestFixture]
    public class BasicNodeTest
    {
        [Test]
        public void DefaultNodeStateIsOk()
        {
            var node = new Node<string>("1");

            Check.ThatEnum(node.Status).IsEqualTo(NodeStatus.Initializing);

            Check.That(node.State.CurrentTerm).IsEqualTo(0L);

            Check.That(node.State.VotedFor).IsNullOrEmpty();

            Check.That(node.State.LogEntries).IsEmpty();
        }
    }
}
