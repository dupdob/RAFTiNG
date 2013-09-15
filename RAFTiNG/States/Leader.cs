//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="Leader.cs" company="Cyrille DUPUYDAUBY">
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
namespace RAFTiNG.States
{
    using RAFTiNG.Messages;

    internal class Leader<T> : State<T>
    {
        public Leader(Node<T> node)
            : base(node)
        {
        }

        internal override void EnterState()
        {
        }

        internal override void ProcessVoteRequest(RequestVote request)
        {
            GrantVote response;
            if (request.Term <= this.Node.State.CurrentTerm)
            {
                // requesting a vote for a node that has less recent information
                // we decline
                this.Logger.TraceFormat(
                    "Received a vote request from a node with a lower term. Message discarded {0}",
                    request);
                response = new GrantVote(false, this.Node.Address, this.Node.State.CurrentTerm);
            }
            else
            {
                if (request.Term > this.Node.State.CurrentTerm)
                {
                    this.Logger.DebugFormat(
                        "Received a vote request from a node with a higher term ({0}'s term is {1}, our {2}). Stepping down.",
                        request.CandidateId,
                        request.Term,
                        this.Node.State.CurrentTerm);

                    // we step down
                    this.Node.SwitchTo(NodeStatus.Follower);

                    // resend the message to process it
                    this.Node.MessageReceived(request);
                    return;
                }

                response = new GrantVote(false, this.Node.Address, this.Node.State.CurrentTerm);
            }

            // send back the response
            this.Node.SendMessage(request.CandidateId, response);
        }

        internal override void ProcessVote(GrantVote vote)
        {
            this.Logger.TraceFormat(
                "Received a vote but we are no longer interested: {0}",
                vote);
        }

        protected override void HeartbeatTimeouted(object state)
        {
        }
    }
}