using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WebInterface.Models
{
	public class ServerStatusModel
	{
		public ServerStatusDirectoryInfo QueueDir = new ServerStatusDirectoryInfo(MantaMTA.Core.MtaParameters.MTA_QUEUEFOLDER);
		public ServerStatusDirectoryInfo LogDir = new ServerStatusDirectoryInfo(MantaMTA.Core.MtaParameters.MTA_SMTP_LOGFOLDER);
		public ServerStatusDirectoryInfo DropDir = new ServerStatusDirectoryInfo(MantaMTA.Core.MtaParameters.MTA_DROPFOLDER);
	}

	public class ServerStatusDirectoryInfo
	{
		public string Path { get; set; }
		public int FileCount
		{
			get
			{
				return Directory.GetFiles(Path).Length;
			}
		}

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

		public ServerStatusDirectoryInfo(string path)
		{
			Path = path;
		}

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