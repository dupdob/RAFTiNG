// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RaftCommunicationSteps.cs" company="Cyrille DUPUYDAUBY">
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

namespace RAFTiNG.Tests
{
    using NFluent;

    using TechTalk.SpecFlow;

    [Binding]
    public class RaftCommunicationSteps
    {
        private Node<string>[] testedNodes;

        private Middleware middleware;

        [Given(@"I have deployed (.*) instance")]
        public void GivenIHaveDeployedInstance(int p0)
        {
            this.middleware = new Middleware();
            this.testedNodes = new Node<string>[p0];
            for (var i = 0; i < p0; i++)
            {
                var testedNode = new Node<string>(i.ToString());
                testedNode.SetMiddleware(this.middleware);
                this.testedNodes[i] = testedNode;
            }
        }
        
        [When(@"I send a message to Node (.*)")]
        public void WhenISendAMessageTo(int p0)
        {
            this.middleware.SendMessage(this.testedNodes[p0 - 1].Address, "message");
        }

        [Then(@"Node (.*) has received my message")]
        public void ThenHasReceivedMyMessage(int p0)
        {
            Check.That(this.testedNodes[p0 - 1].MessagesCount).IsEqualTo(1);
        }
    }
}
