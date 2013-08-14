using System.Configuration;
using System.Data.SqlClient;

namespace MantaMTA.Core.DAL
{
	/// <summary>
	/// Functions to help with database stuff.
	/// </summary>
	internal static class MantaDB
	{
		/// <summary>
		/// Gets a SqlConnection to the MantaMTA Database
		/// </summary>
		/// <returns>Sql connection</returns>
		public static SqlConnection GetSqlConnection()
		{
			return new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString);
		}
	}
}
