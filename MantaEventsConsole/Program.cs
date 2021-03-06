﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MantaMTA.Core.Events;
using CDO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Net.Http;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

namespace MantaEventsConsole
{
	class Program
	{
		private static string RootDirectory = @"C:\temp\Manta\Drop\";

		private static string DirectoryOfBounceEmails = "Return2";
		private static string DirectoryOfFeedbackLoopEmails = "FeedbackLoops";

		private static string SubdirectoryForProblemEmails = "UnableToProcess";



		class EmailsProcessed : ConcurrentDictionary<EmailProcessingDetails, List<string>>
		{
			public void Add(EmailProcessingDetails details, string filename)
			{
				//this.AddOrUpdate(details, new List<string>(new string[] {filename}), (key, oldValue) => oldValue.ToArray()(filename));
				this.AddOrUpdate(details, new List<string>(new string[] { filename }), (key, oldValue) => { oldValue.Add(filename); return oldValue; });
			}
		}


		static EmailsProcessed ProcessedFiles = new EmailsProcessed();

		static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
			// AppDomain.CurrentDomain.FirstChanceException += new EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>(CurrentDomain_FirstChanceException);



			ShowBounceRuleStats();


			Action<string> FeedbackLoopLogger = delegate(string msg) { Console.WriteLine("FeedbackLoop: {0}", msg); };
			Func<string, EmailProcessingDetails> FeedbackLoopProcessor = delegate(string content) { return EventsManager.Instance.ProcessFeedbackLoop(content); };


			Action<string> BounceLogger = delegate(string msg) { Console.WriteLine("Bounce: {0}", msg); };
			Func<string, EmailProcessingDetails> BounceProcessor = delegate(string content) { return EventsManager.Instance.ProcessBounceEmail(content); };


			






			// Ensure directories exist.
			Directory.CreateDirectory(Path.Combine(RootDirectory, DirectoryOfBounceEmails));
			Directory.CreateDirectory(Path.Combine(RootDirectory, DirectoryOfBounceEmails, SubdirectoryForProblemEmails));
			Directory.CreateDirectory(Path.Combine(RootDirectory, DirectoryOfFeedbackLoopEmails));
			Directory.CreateDirectory(Path.Combine(RootDirectory, DirectoryOfFeedbackLoopEmails, SubdirectoryForProblemEmails));

			Directory.CreateDirectory(Path.Combine(RootDirectory, DirectoryOfBounceEmails, "ProcessedSuccessfully"));

			Stopwatch sw = new Stopwatch();
			sw.Start();


			// Process anything that's currently waiting in the directories.
			ProcessBounceFiles(Path.Combine(RootDirectory, DirectoryOfBounceEmails));

			// ProcessFeedbackLoopFiles(Path.Combine(RootDirectory, DirectoryOfFeedbackLoopEmails));

			ShowBounceRuleStats();

			sw.Stop();


			Console.WriteLine("Time taken: {0}", sw.Elapsed);


			Console.WriteLine("{0}How files were processed:{0}", Environment.NewLine);

			foreach(KeyValuePair<EmailProcessingDetails, List<string>> d in ProcessedFiles.OrderByDescending(p => p.Value.Count))
			{
				Console.Write("{0:N0}\t{1}\t", d.Value.Count, d.Key.BounceIdentifier);

				switch (d.Key.BounceIdentifier)
				{
					case MantaMTA.Core.Enums.BounceIdentifier.BounceRule:
						Console.Write(d.Key.MatchingBounceRuleID.ToString());
						break;

					case MantaMTA.Core.Enums.BounceIdentifier.NdrCode:
					case MantaMTA.Core.Enums.BounceIdentifier.SmtpCode:
						Console.Write(d.Key.MatchingValue);
						break;
				}


				Console.WriteLine();
				
			}

			Console.ReadLine();

			return;




			// Create FileSystemWatchers that check for any email files being created so we should leap into action.

			FileSystemWatcher bounceWatcher = new FileSystemWatcher(Path.Combine(RootDirectory, DirectoryOfBounceEmails));
			bounceWatcher.Created += new FileSystemEventHandler(bounceWatcher_Created);
			bounceWatcher.EnableRaisingEvents = true;

			FileSystemWatcher feedbackLoopWatcher = new FileSystemWatcher(Path.Combine(RootDirectory, DirectoryOfFeedbackLoopEmails));
			feedbackLoopWatcher.Created += new FileSystemEventHandler(feedbackLoopWatcher_Created);
			//feedbackLoopWatcher.EnableRaisingEvents = true;
			feedbackLoopWatcher.EnableRaisingEvents = false;
			

			Console.WriteLine("FileSystemWatchers running.");



			// Keep going until a key is pressed.
			Console.ReadKey(true);


			ShowBounceRuleStats();


			if (Debugger.IsAttached)
			{
				Console.WriteLine("{0}Done.  Press Enter to quit.", Environment.NewLine);
				Console.ReadLine();
			}
		}

		private static void ShowBounceRuleStats()
		{
			Console.WriteLine("Bounce Rules:");
			foreach (BounceRule r in BounceRulesManager.BounceRules.OrderByDescending(r => r.Hits))
			{
				Console.WriteLine("Hits:\t{0:N0}\t {1}) {2}", r.Hits, r.RuleID, r.Name);
			}
			Console.WriteLine();
		}


		static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
		{
			ExceptionHandler(sender, e);
		}

		static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
		{
			ExceptionHandler(sender, e);
		}


			
		static void ExceptionHandler(object sender, EventArgs e)
		{
			Console.WriteLine("Exception Thrown");


			if (e is FirstChanceExceptionEventArgs)
			{
				FirstChanceExceptionEventArgs fceArgs = e as FirstChanceExceptionEventArgs;

				Console.WriteLine("Message:\t{0}", fceArgs.Exception.Message);
				Console.WriteLine("Source:\t\t{0}", fceArgs.Exception.Source);
				Console.WriteLine("Stack:\t\t{0}", fceArgs.Exception.StackTrace);
			}
			else if (e is UnhandledExceptionEventArgs && (e as UnhandledExceptionEventArgs).ExceptionObject is Exception)
			{
				UnhandledExceptionEventArgs ueArgs = e as UnhandledExceptionEventArgs;

				Console.WriteLine("Message:\t{0}", (ueArgs.ExceptionObject as Exception).Message);
				Console.WriteLine("Source:\t\t{0}", (ueArgs.ExceptionObject as Exception).Source);
				Console.WriteLine("Stack:\t\t{0}", (ueArgs.ExceptionObject as Exception).StackTrace);
			}
			else
			{
				Console.WriteLine("[unidentified exception thrown]");
			}

			if (Debugger.IsAttached)
			{
				Console.WriteLine("{0}Exception caught.  Press Enter to continue...", Environment.NewLine);
				Console.ReadLine();
			}
		}




		/// <summary>
		/// Object to lock on when checking if the BounceFileWatcher is currently running.
		/// </summary>
		private static object _BounceFileWatcherLock = new object();
		/// <summary>
		/// Indicates whether the bounceWatcher_Created method has been called so shouldn't be called again until it completes.
		/// </summary>
		private static bool _BounceFileWatcherCalled = false;
		/// <summary>
		/// Method to handle the BounceWatcher being told by the o/s that a file has been created.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		static void bounceWatcher_Created(object sender, FileSystemEventArgs e)
		{
			lock (_BounceFileWatcherLock)
			{
				if (_BounceFileWatcherCalled == true)
				{
					Console.WriteLine("BounceFileWatcher already running.");
					return;
				}

				Console.WriteLine("BounceFileWatcher called ({0})", e.Name);
				_BounceFileWatcherCalled = true;
			}
			
			FileSystemWatcher fsw = sender as FileSystemWatcher;

			// Stop the FileSystemWatcher as we'll handle anything we find in the directory until it's empty.
			fsw.EnableRaisingEvents = false;

			try
			{
				// Stay in here until we're done.
				ProcessBounceFiles(Path.GetDirectoryName(e.FullPath));
			}
			catch(Exception)
			{
				Console.WriteLine("Exception processing file!");
			}
			finally
			{
				_BounceFileWatcherCalled = false;
				Console.WriteLine("BounceFileWatcher completed ({0})", e.Name);

				// And resume the listening for events.
				fsw.EnableRaisingEvents = true;
				Console.WriteLine("No more files found; waiting for FileSystemWatcher.");
			}
		}


		/// <summary>
		/// Object to lock on when checking if the FeedbackLoopFileWatcher is currently running.
		/// </summary>
		private static object _FeedbackLoopFileWatcherLock = new object();
		/// <summary>
		/// Indicates whether the FeedbackLoopWatcher_Created method has been called so shouldn't be called again until it completes.
		/// </summary>
		private static bool _FeedbackLoopFileWatcherCalled = false;
		/// <summary>
		/// Method to handle the FeedbackLoopWatcher being told by the o/s that a file has been created.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		static void feedbackLoopWatcher_Created(object sender, FileSystemEventArgs e)
		{
			lock (_FeedbackLoopFileWatcherLock)
			{
				if (_FeedbackLoopFileWatcherCalled == true)
					return;

				_FeedbackLoopFileWatcherCalled = true;
			}

			FileSystemWatcher fsw = sender as FileSystemWatcher;
			
			// Stop the FileSystemWatcher as we'll handle anything we find in the directory until it's empty.
			fsw.EnableRaisingEvents = false;

			try
			{
				// Stay in here until we're done.
				ProcessFeedbackLoopFiles(Path.GetDirectoryName(e.FullPath));
			}
			catch (Exception)
			{

			}
			finally
			{
				_FeedbackLoopFileWatcherCalled = false;

				// And resume the listening for events.
				fsw.EnableRaisingEvents = true;
				Console.WriteLine("No more files found; waiting for FileSystemWatcher.");
			}
		}


		/// <summary>
		/// Processes emails that have been received as bounces.
		/// </summary>
		/// <param name="path">The path to email files to process.</param>
		static void ProcessBounceFiles(string path)
		{
			Action<string> Logger = delegate(string msg) { Console.WriteLine("Bounce: {0}", msg); };
			Func<string, EmailProcessingDetails> Processor = new Func<string, EmailProcessingDetails>(delegate(string content) { return EventsManager.Instance.ProcessBounceEmail(content); });


			DirectoryHandler(path, Processor, Logger);
		}

		
		/// <summary>
		/// Processes emails that have come in from Feedback Loops, indicating spam complaints.
		/// </summary>
		/// <param name="path">The path to email files to process.</param>
		static void ProcessFeedbackLoopFiles(string path)
		{
			Action<string> Logger = delegate(string msg) { Console.WriteLine("FeedbackLoop: {0}", msg); };
			Func<string, EmailProcessingDetails> Processor = new Func<string, EmailProcessingDetails>(delegate(string content) { return EventsManager.Instance.ProcessFeedbackLoop(content); });

			DirectoryHandler(path, Processor, Logger);
		}


		/// <summary>
		/// DirectoryHandler provides a standard way of processing a directory of email files.
		/// </summary>
		/// <param name="path">The filepath to operate on.</param>
		/// <param name="fileProcessor">A delegate method that will be used to process each file found in <paramref name="path"/>.</param>
		/// <param name="logger">A delegate method that will be used to return information to an interface, e.g. to
		/// display messages to a user.</param>
		internal static void DirectoryHandler(string path, Func<string, EmailProcessingDetails> fileProcessor, Action<string> logger)
		{
			// A filter to use when pulling out files to process; likely to be "*.eml".
			string fileSearchPattern = "*.eml";


			FileInfo[] files = new DirectoryInfo(path).GetFiles(fileSearchPattern);
			logger(String.Format("Found {0:N0} files.", files.Count()));


			// Keep going until there aren't any more files to process, then we wait for the FileSystemWatcher to nudge us
			// back into life again.
			do
			{
				// Loop through and process all the files we've picked up.
				//Parallel.ForEach<FileInfo>(files, new Action<FileInfo>(f => 
				foreach(FileInfo f in files)
				{
					try
					{
						FileHandler(f, fileProcessor, logger);
					}
					catch (Exception ex)
					{
						Console.WriteLine("** Error processing file: {0}", f.Name);
						Console.Write("** Press Enter to continue...");
						Console.ReadLine();
					}
				}
				//));



				// Get any new files that have turned up.
				files = new DirectoryInfo(path).GetFiles(fileSearchPattern);

				logger(String.Format("Found {0:N0} files.", files.Count()));
			}
			while (files.Count() > 0);
		}



		/// <summary>
		/// FileHandler provides a standard way of processing a file such as a Bounce or FeedbackLoop email.
		/// </summary>
		internal static Action<FileInfo, Func<string, EmailProcessingDetails>, Action<string>> FileHandler = new Action<FileInfo, Func<string, EmailProcessingDetails>, Action<string>>(delegate(FileInfo f, Func<string, EmailProcessingDetails> processor, Action<string> logger)
		{
			string content = string.Empty;
			EmailProcessingDetails processingDetails = new EmailProcessingDetails();

			logger(String.Format("Processing: {0}", f.FullName));


			if (!File.Exists(f.FullName))
			{
				logger(String.Format("File not found: \"{0}\".", f.FullName));

				return;
			}

			// If a file's not accessible, skip it so we'll pick it up the next time.
			if (IsFileLocked(f))
				return;




			content = File.ReadAllText(f.FullName);

			// Send the content to the delegate method that'll process its contents.
			processingDetails = processor(content);

			ProcessedFiles.Add(processingDetails, f.Name);



			switch (processingDetails.ProcessingResult)
			{
				case EmailProcessingResult.SuccessAbuse:
					 File.Move(f.FullName, Path.Combine(Path.GetDirectoryName(f.FullName), "ProcessedSuccessfully", f.Name));
					break;

				case EmailProcessingResult.SuccessBounce:
					// All good.  Nothing to do other than delete the file.
					//File.Delete(f.FullName);

					string path = Path.Combine(Path.GetDirectoryName(f.FullName), "ProcessedSuccessfully", processingDetails.BounceIdentifier.ToString());

					switch (processingDetails.BounceIdentifier)
					{
						case MantaMTA.Core.Enums.BounceIdentifier.BounceRule:
							path = Path.Combine(path, processingDetails.MatchingBounceRuleID.ToString());
							break;

						case MantaMTA.Core.Enums.BounceIdentifier.NdrCode:
						case MantaMTA.Core.Enums.BounceIdentifier.SmtpCode:
							path = Path.Combine(path, processingDetails.MatchingValue);
							break;
					}



					Directory.CreateDirectory(path);

					File.Move(f.FullName, Path.Combine(path, f.Name));

					// File.Move(f.FullName, Path.Combine(Path.GetDirectoryName(f.FullName), "ProcessedSuccessfully", f.Name));
					break;
					 
				case EmailProcessingResult.ErrorNoFile:
					throw new FileNotFoundException("Failed to locate file to process: \"" + f.Name + "\".");

				case EmailProcessingResult.Unknown:
				case EmailProcessingResult.ErrorContent:
				case EmailProcessingResult.ErrorNoReason:
				default:
					// Move the file into a separate directory, handling any issues of duplicate filenames.
					bool moved = false;
					int version = 0;
					System.Diagnostics.Stopwatch sw = new Stopwatch();
					sw.Start();
					while (!moved)
					{
						try
						{
							if (sw.Elapsed > TimeSpan.FromSeconds(10))
								throw new TimeoutException();

							if (version > 0)
							{
								File.Move(f.FullName, Path.Combine(Path.GetDirectoryName(f.FullName), SubdirectoryForProblemEmails, Path.GetFileNameWithoutExtension(f.Name) + "_" + version.ToString() + f.Extension));
							}
							else
								File.Move(f.FullName, Path.Combine(Path.GetDirectoryName(f.FullName), SubdirectoryForProblemEmails, f.Name));

							moved = true;
						}
						catch (TimeoutException)
						{
							logger("Tried to move file " + f.Name + " for 10 seconds but failed.");
							logger("This is a FATAL failure.");
							Environment.Exit(-1);
						}
						catch (Exception)
						{
							logger("Attempt " + version.ToString() + " Failed.");
							version++;
						}
					}
					break;
			}

			// logger(String.Format("Processed: {0}", f.FullName));
		});



		/// <summary>
		/// Checks whether a file can be opened.
		/// </summary>
		/// <param name="file">The file to check.</param>
		/// <returns>true if the file is locked, else false.</returns>
		static bool IsFileLocked(FileInfo file)
		{
			FileStream stream = null;
			try
			{
				stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException)
			{
				return true;
			}
			finally
			{
				if (stream != null)
					stream.Close();
			}   
			return false;
		}


		static IMessage OpenAsCdoMessage(string filePath)
		{
			IMessages msgs = new DropDirectory().GetMessages(Path.GetDirectoryName(filePath));
			IMessage found = null;


			foreach(IMessage m in msgs)
			{
				if (msgs.get_FileName(m) == filePath)
				{
					found = m;
					break;
				}
			}


			if (found == null)
			{
				Console.WriteLine("Failed to find message {0}.", filePath);
				return null;
			}

			return found;
		}
	}
}