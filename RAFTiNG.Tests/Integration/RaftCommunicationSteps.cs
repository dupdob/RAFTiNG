namespace RAFTiNG.Tests
{
    using System;

    using NFluent;

    using TechTalk.SpecFlow;

    [Binding]
    public class RaftCommunicationSteps
    {
        private Node[] testedNodes;

        private Middleware middleware;

        [Given(@"I have deployed (.*) instance")]
        public void GivenIHaveDeployedInstance(int p0)
        {
            this.middleware = new Middleware();
            this.testedNodes = new Node[p0];
            for (var i = 0; i < p0; i++)
            {
                this.testedNodes[i] = new Node(i.ToString());
            }
        }
        
        [When(@"I send a message to Node (.*)")]
        public void WhenISendAMessageTo(int p0)
        {
            this.middleware.SendMessage("message", this.testedNodes[p0-1].Address);
        }
        
        [Then(@"Node (.*) has received my message")]
        public void ThenHasReceivedMyMessage(int p0)
        {
            Check.That(this.testedNodes[p0 -1].MessagesCount).IsEqualTo(1);
        }
    }

    /// <summary>
    /// Middleware simulates the message middleware used by rafting nodes to communicate and synchronize
    /// </summary>
    public class Middleware
    {
        public void SendMessage(string message, string address)
        {
            throw new NotImplementedException();
        }
    }
}
