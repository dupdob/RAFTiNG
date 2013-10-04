// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BasicRaftSteps.cs" company="Cyrille DUPUYDAUBY">
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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using NFluent;

    using TechTalk.SpecFlow;
    
    public class RaftingInfra: IDisposable
    {
        public Middleware Middleware { get; set; }

        public List<Node<string>> Nodes { get; set; }

        private IDisposable logHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="RaftingInfra"/> class.
        /// </summary>
        public RaftingInfra()
        {
            logHandle = Helpers.InitLog4Net();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var node in this.Nodes)
            {
                node.Dispose();
            }
            this.Nodes.Clear();
            logHandle.Dispose();
        }
    }

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented",
        Justification = "Reviewed. Suppression is OK here.")][Binding]
    public class BasicRaftSteps
    {

        private RaftingInfra infra;
        
        public BasicRaftSteps(RaftingInfra infra)
        {
            this.infra = infra;
        }

        [Given(@"I have deployed (.*) instances")]
        public void GivenIHaveDeployedInstances(int p0)
        {
            var names = new List<string>(p0);
            for (var i = 0; i < p0; i++)
            {
                names.Add(i.ToString(CultureInfo.InvariantCulture));
            }

            this.infra.Middleware = new Middleware();
            this.infra.Nodes = new List<Node<string>>(p0);
            var settings = new NodeSettings();
            settings.Nodes = names.ToArray();
            settings.TimeoutInMs = 100;

            for (var i = 0; i < p0; i++)
            {
                settings.NodeId = names[i];
                var node = new Node<string>(settings);
                node.SetMiddleware(this.infra.Middleware);
                this.infra.Nodes.Add(node);
            }
        }
        
        [When(@"I start instances (.*), (.*) and (.*)")]
        public void WhenIStartInstancesAnd(int p0, int p1, int p2)
        {
            if (p0>0)
            {
                this.infra.Nodes[p0 - 1].Initialize();
            }
            if (p1 == 0)
            {
                return;
            }
            this.infra.Nodes[p1 - 1].Initialize();
            if (p2 == 0)
                return;
            this.infra.Nodes[p2 - 1].Initialize();
        }

        [When(@"I start all instances")]
        public void WhenIStartAllInstances()
        {
            foreach (var node in infra.Nodes)
            {
                node.Initialize();
            }
        }

        [Then(@"there is (.*) leader")]
        public void ThenThereIsLeader(int p0)
        {
            var leaders = this.infra.Nodes.Count(node => node.Status == NodeStatus.Leader);

            Check.That(leaders).IsEqualTo(p0);
        }

        [When(@"I wait (.*) seconde")]
        public void WhenIWaitSeconde(int p0)
        {
            Thread.Sleep(p0 * 1000);
        }
    }
}
