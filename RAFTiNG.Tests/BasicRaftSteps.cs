using System;
using TechTalk.SpecFlow;

namespace RAFTiNG.Tests
{
    using System.Threading;

    using NFluent;

    [Binding]
    public class BasicRaftSteps
    {
        [Given(@"I have deployed (.*) instances")]
        public void GivenIHaveDeployedInstances(int p0)
        {
          
        }
        
        [When(@"I start instances (.*), (.*) and (.*)")]
        public void WhenIStartInstancesAnd(int p0, int p1, int p2)
        {
           
        }
        
        [Then(@"there is (.*) leader")]
        public void ThenThereIsLeader(int p0)
        {
            Check.That(0).IsEqualTo(p0);
        }

        [When(@"I wait (.*) seconde")]
        public void WhenIWaitSeconde(int p0)
        {
            Thread.Sleep(p0 * 1000);
        }
    }
}
