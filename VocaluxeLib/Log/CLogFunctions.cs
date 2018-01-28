#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Serilog.Context;
using Serilog.Core;

namespace VocaluxeLib.Log
{
    public static partial class CLog
    {
        private static Regex _PropertiesRegex = new Regex("{[^}]+}");

        public static object[] Params(params object[] propertyValues)
        {
            return propertyValues;
        }


        #region Verbose functions

        /// <summary>
        /// Write an event with the Verbose level to the Verbose log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Verbose(string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {                
                _MainLog.Verbose(messageTemplate);                
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }

        /// <summary>
        /// Write an event with the Verbose level with additional propertyValues to the Verbose log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Verbose(string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Verbose(messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Verbose(messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        /// <summary>
        /// Write an event with the Verbose level and associated exception to the Verbose log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Verbose(Exception exception, string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                _MainLog.Verbose( exception, messageTemplate);
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }
        
        /// <summary>
        /// Write an event with the Verbose level with additional propertyValues and associated exception to the Verbose log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Verbose(Exception exception, string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Verbose(exception, messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Verbose(exception, messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        #endregion

        #region Debug functions

        /// <summary>
        /// Write an event with the Debug level to the Debug log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Debug(string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {                
                _MainLog.Debug(messageTemplate);                
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }

        /// <summary>
        /// Write an event with the Debug level with additional propertyValues to the Debug log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Debug(string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Debug(messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Debug(messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        /// <summary>
        /// Write an event with the Debug level and associated exception to the Debug log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Debug(Exception exception, string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                _MainLog.Debug( exception, messageTemplate);
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }
        
        /// <summary>
        /// Write an event with the Debug level with additional propertyValues and associated exception to the Debug log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Debug(Exception exception, string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Debug(exception, messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Debug(exception, messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        #endregion

        #region Information functions

        /// <summary>
        /// Write an event with the Information level to the Information log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Information(string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {                
                _MainLog.Information(messageTemplate);                
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }

        /// <summary>
        /// Write an event with the Information level with additional propertyValues to the Information log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Information(string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Information(messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Information(messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        /// <summary>
        /// Write an event with the Information level and associated exception to the Information log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Information(Exception exception, string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                _MainLog.Information( exception, messageTemplate);
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }
        
        /// <summary>
        /// Write an event with the Information level with additional propertyValues and associated exception to the Information log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Information(Exception exception, string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Information(exception, messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Information(exception, messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        #endregion

        #region Warning functions

        /// <summary>
        /// Write an event with the Warning level to the Warning log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Warning(string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {                
                _MainLog.Warning(messageTemplate);                
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }

        /// <summary>
        /// Write an event with the Warning level with additional propertyValues to the Warning log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Warning(string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Warning(messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Warning(messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        /// <summary>
        /// Write an event with the Warning level and associated exception to the Warning log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Warning(Exception exception, string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                _MainLog.Warning( exception, messageTemplate);
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }
        
        /// <summary>
        /// Write an event with the Warning level with additional propertyValues and associated exception to the Warning log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Warning(Exception exception, string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Warning(exception, messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Warning(exception, messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        #endregion

        #region Error functions

        /// <summary>
        /// Write an event with the Error level to the Error log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Error(string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {                
                _MainLog.Error(messageTemplate);                
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }

        /// <summary>
        /// Write an event with the Error level with additional propertyValues to the Error log.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Error(string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Error(messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Error(messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        /// <summary>
        /// Write an event with the Error level and associated exception to the Error log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Error(Exception exception, string messageTemplate, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                _MainLog.Error( exception, messageTemplate);
            }
            if(show)
                ShowLogAssistant(messageTemplate, null);
        }
        
        /// <summary>
        /// Write an event with the Error level with additional propertyValues and associated exception to the Error log.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Error(Exception exception, string messageTemplate, object[] propertyValues, bool show = false, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Error(exception, messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Error(exception, messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues);
        }

        #endregion

        #region Fatal functions

        /// <summary>
        /// Write an event with the Fatal level to the Fatal log and TERMINATES the appication.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Fatal(string messageTemplate, bool show = true, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {                
                _MainLog.Fatal(messageTemplate);                
            }
            if(show)
                ShowLogAssistant(messageTemplate, null, true, false);

            // Close logs and exit
            Close();
            Environment.Exit(Environment.ExitCode);
        }

        /// <summary>
        /// Write an event with the Fatal level with additional propertyValues to the Fatal log and TERMINATES the appication.
        /// </summary>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Fatal(string messageTemplate, object[] propertyValues, bool show = true, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Fatal(messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Fatal(messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues, true, false);

            // Close logs and exit
            Close();
            Environment.Exit(Environment.ExitCode);
        }

        /// <summary>
        /// Write an event with the Fatal level and associated exception to the Fatal log and TERMINATES the appication.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Fatal(Exception exception, string messageTemplate, bool show = true, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                _MainLog.Fatal( exception, messageTemplate);
            }
            if(show)
                ShowLogAssistant(messageTemplate, null, true, false);

            // Close logs and exit
            Close();
            Environment.Exit(Environment.ExitCode);
        }
        
        /// <summary>
        /// Write an event with the Fatal level with additional propertyValues and associated exception to the Fatal log and TERMINATES the appication.
        /// </summary>
        /// <param name="exception">Exception of this event.</param>
        /// <param name="messageTemplate">Message template for this event.</param>
        /// <param name="propertyValues">Data inserted into the message template.</param>
        /// <param name="show">True if an message should be shown to the user, false otherwise.</param>
        /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
        /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
        [MessageTemplateFormatMethod("messageTemplate")]
        public static void Fatal(Exception exception, string messageTemplate, object[] propertyValues, bool show = true, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
            {
                int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();

                if (propertyValues.Length > usedPropertiesCount)
                {
                    using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                    {
                        _MainLog.Fatal(exception, messageTemplate, propertyValues);
                    }
                }
                else
                {
                    _MainLog.Fatal(exception, messageTemplate, propertyValues);
                }
            }
            if(show)
                ShowLogAssistant(messageTemplate, propertyValues, true, false);

            // Close logs and exit
            Close();
            Environment.Exit(Environment.ExitCode);
        }

        #endregion
    
        [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
        public static class CSongLog 
        {
    
            #region Verbose functions
    
            /// <summary>
            /// Write an event with the Verbose level to the Verbose log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Verbose(string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {                
                    _SongLog.Verbose(messageTemplate);                
                }
            }
    
            /// <summary>
            /// Write an event with the Verbose level with additional propertyValues to the Verbose log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Verbose(string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Verbose(messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Verbose(messageTemplate, propertyValues);
                    }
                }
            }
    
            /// <summary>
            /// Write an event with the Verbose level and associated exception to the Verbose log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Verbose(Exception exception, string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    _SongLog.Verbose( exception, messageTemplate);
                }
            }
            
            /// <summary>
            /// Write an event with the Verbose level with additional propertyValues and associated exception to the Verbose log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Verbose(Exception exception, string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Verbose(exception, messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Verbose(exception, messageTemplate, propertyValues);
                    }
                }
            }
    
            #endregion
    
            #region Debug functions
    
            /// <summary>
            /// Write an event with the Debug level to the Debug log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Debug(string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {                
                    _SongLog.Debug(messageTemplate);                
                }
            }
    
            /// <summary>
            /// Write an event with the Debug level with additional propertyValues to the Debug log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Debug(string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Debug(messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Debug(messageTemplate, propertyValues);
                    }
                }
            }
    
            /// <summary>
            /// Write an event with the Debug level and associated exception to the Debug log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Debug(Exception exception, string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    _SongLog.Debug( exception, messageTemplate);
                }
            }
            
            /// <summary>
            /// Write an event with the Debug level with additional propertyValues and associated exception to the Debug log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Debug(Exception exception, string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Debug(exception, messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Debug(exception, messageTemplate, propertyValues);
                    }
                }
            }
    
            #endregion
    
            #region Information functions
    
            /// <summary>
            /// Write an event with the Information level to the Information log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Information(string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {                
                    _SongLog.Information(messageTemplate);                
                }
            }
    
            /// <summary>
            /// Write an event with the Information level with additional propertyValues to the Information log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Information(string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Information(messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Information(messageTemplate, propertyValues);
                    }
                }
            }
    
            /// <summary>
            /// Write an event with the Information level and associated exception to the Information log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Information(Exception exception, string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    _SongLog.Information( exception, messageTemplate);
                }
            }
            
            /// <summary>
            /// Write an event with the Information level with additional propertyValues and associated exception to the Information log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Information(Exception exception, string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Information(exception, messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Information(exception, messageTemplate, propertyValues);
                    }
                }
            }
    
            #endregion
    
            #region Warning functions
    
            /// <summary>
            /// Write an event with the Warning level to the Warning log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Warning(string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {                
                    _SongLog.Warning(messageTemplate);                
                }
            }
    
            /// <summary>
            /// Write an event with the Warning level with additional propertyValues to the Warning log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Warning(string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Warning(messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Warning(messageTemplate, propertyValues);
                    }
                }
            }
    
            /// <summary>
            /// Write an event with the Warning level and associated exception to the Warning log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Warning(Exception exception, string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    _SongLog.Warning( exception, messageTemplate);
                }
            }
            
            /// <summary>
            /// Write an event with the Warning level with additional propertyValues and associated exception to the Warning log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Warning(Exception exception, string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Warning(exception, messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Warning(exception, messageTemplate, propertyValues);
                    }
                }
            }
    
            #endregion
    
            #region Error functions
    
            /// <summary>
            /// Write an event with the Error level to the Error log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Error(string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {                
                    _SongLog.Error(messageTemplate);                
                }
            }
    
            /// <summary>
            /// Write an event with the Error level with additional propertyValues to the Error log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Error(string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Error(messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Error(messageTemplate, propertyValues);
                    }
                }
            }
    
            /// <summary>
            /// Write an event with the Error level and associated exception to the Error log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Error(Exception exception, string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    _SongLog.Error( exception, messageTemplate);
                }
            }
            
            /// <summary>
            /// Write an event with the Error level with additional propertyValues and associated exception to the Error log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Error(Exception exception, string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    int usedPropertiesCount = _PropertiesRegex.Matches(messageTemplate).Cast<Match>().Select(m => m.Value).Distinct().Count();
    
                    if (propertyValues.Length > usedPropertiesCount)
                    {
                        using (LogContext.PushProperty("AdditionalData", propertyValues.Skip(usedPropertiesCount)))
                        {
                            _SongLog.Error(exception, messageTemplate, propertyValues);
                        }
                    }
                    else
                    {
                        _SongLog.Error(exception, messageTemplate, propertyValues);
                    }
                }
            }
    
            #endregion
        }
    }
}