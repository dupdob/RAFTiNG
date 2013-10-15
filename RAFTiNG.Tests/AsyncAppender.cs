//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncAppender.cs" company="Cyrille DUPUYDAUBY">
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
namespace RAFTiNG.Tests
{
    using System.Collections.Generic;
    using System.Threading;

    using log4net.Appender;
    using log4net.Core;
    using log4net.Util;

    /// <summary>
    /// Implements an forward appender executing asynchronously
    /// </summary>
    public class AsyncAppender : ForwardingAppender
    {
        private readonly Thread appenderThread;
        private readonly object synchro = new object();

        private List<LoggingEvent> events;

        private bool stop;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:log4net.Appender.ForwardingAppender"/> class.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Default constructor.
        /// </para>
        /// </remarks>
        public AsyncAppender()
        {
            appenderThread = new Thread(this.AppenderLoop);
            appenderThread.Start();
        }

        /// <summary>
        /// Initialize the appender based on the options set
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is part of the <see cref="T:log4net.Core.IOptionHandler"/> delayed object
        ///             activation scheme. The <see cref="M:log4net.Appender.AppenderSkeleton.ActivateOptions"/> method must 
        ///             be called on this object after the configuration properties have
        ///             been set. Until <see cref="M:log4net.Appender.AppenderSkeleton.ActivateOptions"/> is called this
        ///             object is in an undefined state and must not be used. 
        /// </para>
        /// <para>
        /// If any of the configuration properties are modified then 
        ///             <see cref="M:log4net.Appender.AppenderSkeleton.ActivateOptions"/> must be called again.
        /// </para>
        /// </remarks>
        public override void ActivateOptions()
        {
            base.ActivateOptions();

        }

        /// <summary>
        /// Closes the appender and releases resources.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Releases any resources allocated within the appender such as file handles, 
        ///             network connections, etc.
        /// </para>
        /// <para>
        /// It is a programming error to append to a closed appender.
        /// </para>
        /// </remarks>
        protected override void OnClose()
        {
            lock (this.synchro)
            {
                stop = true;
                Monitor.Pulse(this.synchro);
            }

            if (Thread.CurrentThread != this.appenderThread)
            {
                if (!this.appenderThread.Join(2000))
                {
                    LogLog.Warn(this.GetType(), "Failed to stop appender thread");
                }
            }
            base.OnClose();
        }

        /// <summary>
        /// Forward the logging event to the attached appenders 
        /// </summary>
        /// <param name="loggingEvent">The event to log.</param>
        /// <remarks>
        /// <para>
        /// Delivers the logging event to all the attached appenders.
        /// </para>
        /// </remarks>
        protected override void Append(LoggingEvent loggingEvent)
        {
            loggingEvent.Fix = FixFlags.ThreadName;
            lock (this.synchro)
            {
                if (this.events == null)
                {
                    this.events = new List<LoggingEvent> { loggingEvent };
                    Monitor.Pulse(this.synchro);
                }
                else
                {
                    this.events.Add(loggingEvent);
                }
            }
        }

        /// <summary>
        /// Forward the logging events to the attached appenders 
        /// </summary>
        /// <param name="loggingEvents">The array of events to log.</param>
        /// <remarks>
        /// <para>
        /// Delivers the logging events to all the attached appenders.
        /// </para>
        /// </remarks>
        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
            {
                loggingEvent.Fix = FixFlags.ThreadName;                
            }

            lock (this.synchro)
            {
                if (this.events == null)
                {
                    this.events = new List<LoggingEvent>(loggingEvents);
                    Monitor.Pulse(this.synchro);
                }
                else
                {
                    this.events.AddRange(loggingEvents);
                }
            }
        }

        private void AppenderLoop()
        {
            for (;;)
            {
                List<LoggingEvent> newEvents;
                lock (this.synchro)
                {
                    if (!this.stop && this.events == null)
                    {
                        Monitor.Wait(this.synchro);
                    }
                    if (this.stop | this.events == null)
                    {
                        return;
                    }
                    newEvents = this.events;
                    this.events = null;
                }

                foreach (var appender in Appenders)
                {
                    var bulk = appender as IBulkAppender;
                    if (bulk != null)
                    {
                        bulk.DoAppend(newEvents.ToArray());
                    }
                    else
                    {
                        foreach (var loggingEvent in newEvents)
                        {
                            appender.DoAppend(loggingEvent);
                        }
                    }
                }
            }
        }

    }
}