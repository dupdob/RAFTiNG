//  --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogExtensions.cs" company="Cyrille DUPUYDAUBY">
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
namespace RAFTiNG
{
    using System.Reflection;

    using log4net;
    using log4net.Core;

    public static class LogExtensions
    {
        /// <summary>
        /// Log a trace level event.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="format">The format.</param>
        /// <param name="formatItems">The format items.</param>
         public static void TraceFormat(
             this ILog logger, string format, params object[] formatItems)
         {
             logger.Logger.Log(MethodBase.GetCurrentMethod().DeclaringType, Level.Trace, string.Format(format, formatItems), null);
         }
    }
}