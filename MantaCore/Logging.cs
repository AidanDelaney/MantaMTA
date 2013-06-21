using System;
using log4net;

namespace MantaMTA.Core
{
	/// <summary>
	/// Logging is used by the application to log messages into the log4net framework.
	/// </summary>
	public static class Logging
	{
		/// <summary>
		/// The logger.
		/// </summary>
		private static readonly ILog _log = LogManager.GetLogger(MtaParameters.MTA_NAME);

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Debug level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		public static void Debug(object msg)
		{
			if (_log.IsDebugEnabled)
				_log.Debug(msg);
		}

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Debug level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		/// <param name="ex">The exception to log, including its stack trace.</param>
		public static void Debug(object msg, Exception ex)
		{
			if (_log.IsDebugEnabled)
				_log.Debug(msg, ex);
		}

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Error level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		public static void Error(object msg)
		{
			if (_log.IsErrorEnabled)
				_log.Error(msg);
		}

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Error level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		/// <param name="ex">The exception to log, including its stack trace.</param>
		public static void Error(object msg, Exception ex)
		{
			if (_log.IsErrorEnabled)
				_log.Error(msg, ex);
		}

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Fatal level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		public static void Fatal(object msg)
		{
			if (_log.IsFatalEnabled)
				_log.Fatal(msg);
		}

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Fatal level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		/// <param name="ex">The exception to log, including its stack trace.</param>
		public static void Fatal(object msg, Exception ex)
		{
			if (_log.IsFatalEnabled)
				_log.Fatal(msg, ex);
		}

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Info level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		public static void Info(object msg)
		{
			if(_log.IsInfoEnabled)
				_log.Info(msg);
		}

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Info level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		/// <param name="ex">The exception to log, including its stack trace.</param>
		public static void Info(object msg, Exception ex)
		{
			if (_log.IsInfoEnabled)
				_log.Info(msg, ex);
		}

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Warn level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		public static void Warn(object msg)
		{
			if (_log.IsWarnEnabled)
				_log.Warn(msg);
		}

		/// <summary>
		/// Log a message object with the log4net.Core.Level.Warn level.
		/// </summary>
		/// <param name="msg">The message object to log.</param>
		/// <param name="ex">The exception to log, including its stack trace.</param>
		public static void Warn(object msg, Exception ex)
		{
			if (_log.IsWarnEnabled)
				_log.Warn(msg, ex);
		}
	}
}
