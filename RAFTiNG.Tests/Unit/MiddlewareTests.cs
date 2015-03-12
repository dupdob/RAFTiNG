// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MiddlewareTests.cs" company="Cyrille DUPUYDAUBY">
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
    using System;

    using NFluent;

    using NUnit.Framework;

    using RAFTiNG.Tests.Services;

    [TestFixture]
    public class MiddlewareTests
    {
        private object lastMessage;

        [Test]
        public void InitializationTests()
        {
            var test = new Middleware(false);

            Check.That(() => test.RegisterEndPoint("testPoint", this.MessageReceived)).DoesNotThrow();
        }

        [Test]
        public void CheckThatDoubleRegisterFails()
        {
            var test = new Middleware(false);
            test.RegisterEndPoint("test", this.MessageReceived);
            Check.ThatCode(() => test.RegisterEndPoint("test", this.MessageReceived))
                .Throws<InvalidOperationException>();
            ;
        }

        [Test]
        public void CheckThatRegisterEndPointFailsForNullOrEmptyAddress()
        {
            var test = new Middleware(false);
            Check.ThatCode(() => test.RegisterEndPoint(string.Empty, this.MessageReceived)).Throws<ArgumentNullException>();
        }

        [Test]
        public void CheckThatRegisterEndPointFailsForNullHandler()
        {
            var test = new Middleware(false);
            Check.ThatCode( () => test.RegisterEndPoint("test", null)) .Throws<ArgumentNullException>();
        }

        [Test]
        public void CheckThatMessageIsReceived()
        {
            this.lastMessage = null;
            var test = new Middleware(false);

            test.RegisterEndPoint("point", this.MessageReceived);
            var newMessage = new object();
            test.SendMessage("point", newMessage);

            Check.That(this.lastMessage).IsSameReferenceThan(newMessage);
        }

        [Test]
        public void CheckThatExceptionsAreFiltered()
        {
            var test = new Middleware(false);

            test.RegisterEndPoint("test", x => { throw new Exception(); });
            Check.That( ()=> test.SendMessage("test", 1)).DoesNotThrow();
        }

        private void MessageReceived(object message)
        {
            this.lastMessage = message;
        }
    }
}
