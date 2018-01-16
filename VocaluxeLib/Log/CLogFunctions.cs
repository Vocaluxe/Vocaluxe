using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Serilog.Context;
using Serilog.Core;

namespace VocaluxeLib.Log
{
    public static partial class CLog
    {

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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Verbose(messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Verbose(exception, messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Debug(messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Debug(exception, messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Information(messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Information(exception, messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Warning(messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Warning(exception, messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Error(messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
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
                _MainLog.Error(exception, messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
        }

        #endregion

        #region Fatal functions

        /// <summary>
        /// Write an event with the Fatal level to the Fatal log.
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
        }

        /// <summary>
        /// Write an event with the Fatal level with additional propertyValues to the Fatal log.
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
                _MainLog.Fatal(messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
        }

        /// <summary>
        /// Write an event with the Fatal level and associated exception to the Fatal log.
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
                _ShowLogAssistent(messageTemplate, null, callerMethodeName, callerFilePath, callerLineNumer);
        }
        
        /// <summary>
        /// Write an event with the Fatal level with additional propertyValues and associated exception to the Fatal log.
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
                _MainLog.Fatal(exception, messageTemplate, propertyValues);
            }
            if(show)
                _ShowLogAssistent(messageTemplate, propertyValues, callerMethodeName, callerFilePath, callerLineNumer);
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
                    _SongLog.Verbose(messageTemplate, propertyValues);
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
                    _SongLog.Verbose(exception, messageTemplate, propertyValues);
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
                    _SongLog.Debug(messageTemplate, propertyValues);
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
                    _SongLog.Debug(exception, messageTemplate, propertyValues);
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
                    _SongLog.Information(messageTemplate, propertyValues);
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
                    _SongLog.Information(exception, messageTemplate, propertyValues);
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
                    _SongLog.Warning(messageTemplate, propertyValues);
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
                    _SongLog.Warning(exception, messageTemplate, propertyValues);
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
                    _SongLog.Error(messageTemplate, propertyValues);
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
                    _SongLog.Error(exception, messageTemplate, propertyValues);
                }
            }
    
            #endregion
    
            #region Fatal functions
    
            /// <summary>
            /// Write an event with the Fatal level to the Fatal log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Fatal(string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    _SongLog.Fatal(messageTemplate);
                }
            }
    
            /// <summary>
            /// Write an event with the Fatal level with additional propertyValues to the Fatal log.
            /// </summary>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Fatal(string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    _SongLog.Fatal(messageTemplate, propertyValues);
                }
            }
    
            /// <summary>
            /// Write an event with the Fatal level and associated exception to the Fatal log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Fatal(Exception exception, string messageTemplate, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    _SongLog.Fatal( exception, messageTemplate);
                }
            }
            
            /// <summary>
            /// Write an event with the Fatal level with additional propertyValues and associated exception to the Fatal log.
            /// </summary>
            /// <param name="exception">Exception of this event.</param>
            /// <param name="messageTemplate">Message template for this event.</param>
            /// <param name="propertyValues">Data inserted into the message template.</param>
            /// <param name="callerMethodeName">Don't use! The methode name of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerFilePath">Don't use! The filepath of the caller will be filled automatically by the compiler.</param>
            /// <param name="callerLineNumer">Don't use! The line number of the caller will be filled automatically by the compiler.</param>
            [MessageTemplateFormatMethod("messageTemplate")]
            public static void Fatal(Exception exception, string messageTemplate, object[] propertyValues, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1)
            {
                using (LogContext.PushProperty("CallingContext", new { callerMethodeName, callerFilePath, callerLineNumer}))
                {
                    _SongLog.Fatal(exception, messageTemplate, propertyValues);
                }
            }
    
            #endregion
        }
    }
}