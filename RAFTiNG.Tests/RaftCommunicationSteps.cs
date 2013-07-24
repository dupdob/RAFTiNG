using System;
using TechTalk.SpecFlow;

namespace RAFTiNG.Tests
{
    [Binding]
    public class RaftCommunicationSteps
    {
        [Given(@"I have deployed (.*) instance")]
        public void GivenIHaveDeployedInstance(int p0)
        {
            ScenarioContext.Current.Pending();
        }
        
        [When(@"I send a message to (.*)")]
        public void WhenISendAMessageTo(int p0)
        {
            ScenarioContext.Current.Pending();
        }
        
        [Then(@"(.*) has received my message")]
        public void ThenHasReceivedMyMessage(int p0)
        {
            ScenarioContext.Current.Pending();
        }
    }
}
