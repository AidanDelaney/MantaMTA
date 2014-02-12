using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace WebInterface.Models
{
	/// <summary>
	/// Model for the server status page.
	/// </summary>
	public class ServerStatusModel
	{
		public ServerStatusDirectoryInfo QueueDir = new ServerStatusDirectoryInfo(MantaMTA.Core.MtaParameters.MTA_QUEUEFOLDER);
		public ServerStatusDirectoryInfo LogDir = new ServerStatusDirectoryInfo(MantaMTA.Core.MtaParameters.MTA_SMTP_LOGFOLDER);
		public ServerStatusDirectoryInfo DropDir = new ServerStatusDirectoryInfo(MantaMTA.Core.MtaParameters.MTA_DROPFOLDER);

        public string MantaResetLog = string.Empty;

        public ServerStatusModel()
        {
            List<string> lines = new List<string>();
            using (StreamReader sr = new StreamReader(ConfigurationManager.AppSettings["MantaResetMtaLogPath"]))
            {
                while (sr.Peek() != -1)
                {
                    if (lines.Count == 20)
                        lines.RemoveAt(1);
                    lines.Add(sr.ReadLine());
                }
            }

           // if(lines.Count > 20)
            //    lines = lines.GetRange(lines.Count - 20, 20);

            MantaResetLog = string.Join(Environment.NewLine, lines);
        }
	}

	/// <summary>
	/// A server status directory forms part of the server status page model.
	/// It gets information about a directory.
	/// </summary>
	public class ServerStatusDirectoryInfo
	{
		/// <summary>
		/// Path of the directory.
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// Amount of files in the directory.
		/// </summary>
		public int FileCount
		{
			get
			{
				return Directory.GetFiles(Path).Length;
			}
		}

		/// <summary>
		/// Gets the used space of the directory.
		/// Formatted like : 1 Kb, 1 Mb, 1 Gb
		/// </summary>
		public string UsedSpace
		{
			get
			{
				float dirSize = CalculateDirectorySize(Path);
				if (dirSize < 1024)
					return "0 Kb";

				// Kb
				dirSize = dirSize / 1024;
				if (dirSize < 1024)
					return dirSize.ToString("N0") + " Kb";

				// Mb
				dirSize = dirSize / 1024;
				if (dirSize < 1024)
					return dirSize.ToString("N1") + " Mb";

				// Gb
				dirSize = dirSize / 1024;
				return dirSize.ToString("N1") + " Gb";
			}
		}

		/// <summary>
		/// Gets the free space of the directory's root partition.
		/// Formatted like : 1 Kb, 1 Mb, 1 Gb
		/// </summary>
		public string FreeSpace
		{
			get
			{
				string pathRoot = System.IO.Path.GetPathRoot(Path);
				long freeSpace = DriveInfo.GetDrives().Single(d => d.RootDirectory.ToString().Equals(pathRoot, StringComparison.OrdinalIgnoreCase)).TotalFreeSpace;
				if (freeSpace < 1024)
					return "0 Kb";

				// Kb
				freeSpace = freeSpace / 1024;
				if (freeSpace < 1024)
					return freeSpace.ToString("N0") + " Kb";

				// Mb
				freeSpace = freeSpace / 1024;
				if (freeSpace < 1024)
					return freeSpace.ToString("N1") + " Mb";

				// Gb
				freeSpace = freeSpace / 1024;
				return freeSpace.ToString("N1") + " Gb";
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="path">The path to the directory.</param>
		public ServerStatusDirectoryInfo(string path)
		{
			Path = path;
		}

		/// <summary>
		/// Calculates the size of all the files in a directory and any sub directories.
		/// </summary>
		/// <param name="folder"></param>
		/// <returns></returns>
		private float CalculateDirectorySize(string folder)
		{
			float folderSize = 0.0f;
			try
			{
				//Checks if the path is valid or not
				if (!Directory.Exists(folder))
					return folderSize;
				else
				{
					try
					{
						foreach (string file in Directory.GetFiles(folder))
						{
							if (File.Exists(file))
							{
								FileInfo finfo = new FileInfo(file);
								folderSize += finfo.Length;
							}
						}

						foreach (string dir in Directory.GetDirectories(folder))
							folderSize += CalculateDirectorySize(dir);
					}
					catch (NotSupportedException e)
					{
						Console.WriteLine("Unable to calculate folder size: {0}", e.Message);
					}
				}
			}
			catch (UnauthorizedAccessException e)
			{
				Console.WriteLine("Unable to calculate folder size: {0}", e.Message);
			}
			return folderSize;
		}
	}
}