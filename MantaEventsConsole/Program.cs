using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MantaMTA.Core.Events;

namespace MantaEventsConsole
{
	class Program
	{
		private static string RootDirectory = @"C:\temp\Manta\Drop\";

		private static string DirectoryOfBounceEmails = "Return";
		private static string DirectoryOfFeedbackLoopEmails = "FeedbackLoops";

		private static string SubdirectoryForProblemEmails = "UnableToProcess";


		static void Main(string[] args)
		{
			// AppDomain.CurrentDomain.FirstChanceException += new EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>(CurrentDomain_FirstChanceException);


			Action<string> FeedbackLoopLogger = delegate(string msg) { Console.WriteLine("FeedbackLoop: {0}", msg); };
			Action<string> FeedbackLoopProcessor = delegate(string content){ EventsManager.Instance.ProcessFeedbackLoop(content); };


			Action<string> BounceLogger = delegate(string msg) { Console.WriteLine("Bounce: {0}", msg); };
			Action<string> BounceProcessor = delegate(string content) { EventsManager.Instance.ProcessBounce(content); };


			






			// Ensure directories exist.
			Directory.CreateDirectory(Path.Combine(RootDirectory, DirectoryOfBounceEmails));
			Directory.CreateDirectory(Path.Combine(RootDirectory, DirectoryOfBounceEmails, SubdirectoryForProblemEmails));
			Directory.CreateDirectory(Path.Combine(RootDirectory, DirectoryOfFeedbackLoopEmails));
			Directory.CreateDirectory(Path.Combine(RootDirectory, DirectoryOfFeedbackLoopEmails, SubdirectoryForProblemEmails));



			// Process anything that's currently waiting in the directories.
			ProcessBounceFiles(Path.Combine(RootDirectory, DirectoryOfBounceEmails));
			ProcessFeedbackLoopFiles(Path.Combine(RootDirectory, DirectoryOfFeedbackLoopEmails));




			// Create FileSystemWatchers that check for any email files being created so we should leap into action.

			FileSystemWatcher bounceWatcher = new FileSystemWatcher(Path.Combine(RootDirectory, DirectoryOfBounceEmails));
			bounceWatcher.Created += new FileSystemEventHandler(bounceWatcher_Created);
			bounceWatcher.EnableRaisingEvents = true;

			FileSystemWatcher feedbackLoopWatcher = new FileSystemWatcher(Path.Combine(RootDirectory, DirectoryOfFeedbackLoopEmails));
			feedbackLoopWatcher.Created += new FileSystemEventHandler(feedbackLoopWatcher_Created);
			feedbackLoopWatcher.EnableRaisingEvents = true;
			

			Console.WriteLine("FileSystemWatchers running.");



			// Keep going until a key is pressed.
			Console.ReadKey(true);


			if (Debugger.IsAttached)
			{
				Console.WriteLine("{0}Done.  Press Enter to quit.", Environment.NewLine);
				Console.ReadLine();
			}
		}


		static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
		{
			Console.WriteLine("Exception Thrown");
			Console.WriteLine("Message:\t{0}", e.Exception.Message);
			Console.WriteLine("Source:\t\t{0}", e.Exception.Source);
			Console.WriteLine("Stack:\t\t{0}", e.Exception.StackTrace);
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
					return;

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

			}
			finally
			{
				_BounceFileWatcherCalled = false;

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
			Func<string, EmailProcessingResult> Processor = new Func<string, EmailProcessingResult>(delegate(string content) { return EventsManager.Instance.ProcessBounce(content); });


			DirectoryHandler(path, Processor, Logger);
		}

		
		/// <summary>
		/// Processes emails that have come in from Feedback Loops, indicating spam complaints.
		/// </summary>
		/// <param name="path">The path to email files to process.</param>
		static void ProcessFeedbackLoopFiles(string path)
		{
			Action<string> Logger = delegate(string msg) { Console.WriteLine("FeedbackLoop: {0}", msg); };
			Func<string, EmailProcessingResult> Processor = new Func<string, EmailProcessingResult>(delegate(string content) { return EventsManager.Instance.ProcessFeedbackLoop(content); });

			DirectoryHandler(path, Processor, Logger);
		}


		/// <summary>
		/// DirectoryHandler provides a standard way of processing a directory of email files.
		/// </summary>
		/// <param name="path">The filepath to operate on.</param>
		/// <param name="fileProcessor">A delegate method that will be used to process each file found in <paramref name="path"/>.</param>
		/// <param name="logger">A delegate method that will be used to return information to an interface, e.g. to
		/// display messages to a user.</param>
		internal static void DirectoryHandler(string path, Func<string, EmailProcessingResult> fileProcessor, Action<string> logger)
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
				Parallel.ForEach<FileInfo>(files, new Action<FileInfo>(f => FileHandler(f, fileProcessor, logger)));


				// Get any new files that have turned up.
				files = new DirectoryInfo(path).GetFiles(fileSearchPattern);

				logger(String.Format("Found {0:N0} files.", files.Count()));
			}
			while (files.Count() > 0);
		}



		/// <summary>
		/// FileHandler provides a standard way of processing a file such as a Bounce or FeedbackLoop email.
		/// </summary>
		internal static Action<FileInfo, Func<string, EmailProcessingResult>, Action<string>> FileHandler = new Action<FileInfo, Func<string, EmailProcessingResult>, Action<string>>(delegate(FileInfo f, Func<string, EmailProcessingResult> processor, Action<string> logger)
		{
			string content = string.Empty;
			EmailProcessingResult result = EmailProcessingResult.Unknown;

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
			result = processor(content);


			switch (result)
			{
				case EmailProcessingResult.SuccessAbuse:
				case EmailProcessingResult.SuccessBounce:
					// All good.  Nothing to do other than delete the file.
					File.Delete(f.FullName);
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
	}
}