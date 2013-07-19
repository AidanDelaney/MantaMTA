using System;
using System.IO;

namespace MantaMTA.Core.Smtp
{
	internal class SmtpTransactionLogger : IDisposable
	{
		// Use singleton so only one logging instance will be reading/writing to the file at a time
		private static SmtpTransactionLogger _Instance = new SmtpTransactionLogger();
		public static SmtpTransactionLogger Instance { get { return _Instance; } }
		private SmtpTransactionLogger()
		{
			// Handle any uncaught exceptions, need to flush and close the logging streams
			AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
			{
				// If writer is open and can be written
				// then we should flush and close
				if (_Writer != null &&
					 _Writer.BaseStream != null &&
					 _Writer.BaseStream.CanWrite)
				{
					_Writer.Flush();
					_Writer.Close();
				}
			};
		}

		~SmtpTransactionLogger()
		{
			Dispose();
		}

		public void Dispose()
		{
			try
			{
				if (_Writer != null)
					_Writer.Dispose();
			}
			catch (Exception)
			{

			}
		}

		/// <summary>
		/// Stream writer is used for writing to the log file
		/// </summary>
		private StreamWriter _Writer = null;

		/// <summary>
		/// Identifies the current log hour
		/// </summary>
		private int _CurrentLogHour = -1;

		/// <summary>
		/// Log file writer lock.
		/// </summary>
		private object writeLock = new object();

		/// <summary>
		/// Write a message to the log file
		/// </summary>
		/// <param name="msg"></param>
		public void Log(string msg)
		{
			lock (writeLock)
			{
				// Ensure logging by hour
				if (DateTime.UtcNow.Hour != _CurrentLogHour && _Writer != null)
				{
					_Writer.Flush();
					_Writer.Close();
					_Writer = null;
				}

				// If the stream writer doesn't exist, the filestream doesn't exist or is not writeable
				if (_Writer == null || _Writer.BaseStream == null || !_Writer.BaseStream.CanWrite)
					_Writer = new StreamWriter(GetCurrentLogPath(), true);

				_Writer.WriteLine(GetCurrentTimestamp() + " " + msg.TrimEnd());
				_Writer.Flush();
				_Writer.BaseStream.Flush();
			}
		}

		/// <summary>
		/// Return a string containing the current date/time in the format used
		/// for logging
		/// </summary>
		/// <returns></returns>
		private string GetCurrentTimestamp()
		{
			return DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.ff");
		}

		/// <summary>
		/// Works out the current log file path, log files use date time to the hour
		/// </summary>
		/// <returns></returns>
		private string GetCurrentLogPath()
		{
			_CurrentLogHour = DateTime.UtcNow.Hour;
			return Path.Combine(MtaParameters.MTA_LOGFOLDER , DateTime.UtcNow.ToString("yyyyMMddhh") + ".txt");
		}

	}
}
