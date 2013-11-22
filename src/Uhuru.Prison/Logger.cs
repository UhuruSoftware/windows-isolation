// -----------------------------------------------------------------------
// <copyright file="Logger.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.Prison
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using NLog;
    
    /// <summary>
    /// This is a helper logger class that is used throughout the code.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// The NLog.Logger object used for logging.
        /// </summary>
        private static readonly NLog.Logger log = LogManager.GetLogger(System.AppDomain.CurrentDomain.FriendlyName);

        /// <summary>
        /// Logs a fatal message.
        /// This indicates a really severe error, that will probably make the application crash.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Fatal(string message)
        {
            log.Fatal(message);
        }

        /// <summary>
        /// Logs an error message.
        /// This indicates an error, but the application may be able to continue.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Error(string message)
        {
            log.Error(message);
        }

        /// <summary>
        /// Logs a warning message.
        /// This indicates a situation that could lead to some bad things.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Warning(string message)
        {
            log.Warn(message);
        }

        /// <summary>
        /// Logs an information message.
        /// The message is used to indicate some progress.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Info(string message)
        {
            log.Info(message);
        }

        /// <summary>
        /// Logs a debug message.
        /// This is an informational message, that is useful when debugging.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        public static void Debug(string message)
        {
            if (message != null && message.Contains("connection 0"))
            {
                return;
            }

            log.Debug(message);
        }

        /// <summary>
        /// Logs a fatal message and formats it.
        /// This indicates a really severe error, that will probably make the application crash.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Fatal(string message, params object[] args)
        {
            log.Fatal(CultureInfo.InvariantCulture, message, args);
        }

        /// <summary>
        /// Logs an error message and formats it.
        /// This indicates an error, but the application may be able to continue.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Error(string message, params object[] args)
        {
            log.Error(CultureInfo.InvariantCulture, message, args);
        }

        /// <summary>
        /// Logs a warning message and formats it.
        /// This indicates a situation that could lead to some bad things.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Warning(string message, params object[] args)
        {
            log.Warn(CultureInfo.InvariantCulture, message, args);
        }

        /// <summary>
        /// Logs an information message and formats it.
        /// The message is used to indicate some progress.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Info(string message, params object[] args)
        {
            log.Info(CultureInfo.InvariantCulture, message, args);
        }

        /// <summary>
        /// Logs a debug message and formats it.
        /// This is an informational message, that is useful when debugging.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="args">The arguments used for formatting.</param>
        public static void Debug(string message, params object[] args)
        {
            log.Debug(CultureInfo.InvariantCulture, message, args);
        }
    }
}
