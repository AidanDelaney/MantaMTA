using System;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;

namespace MantaMTA.Core.Tests
{
	public class TestFixtureBase
	{
		/// <summary>
		/// Creates a TransactionScope object with appropriate configuration values.
		/// 
		/// See this article for more info as apparently TransactionScope objects can be tricky with their default
		/// constructor values:
		/// http://blogs.msdn.com/b/dbrowne/archive/2010/05/21/using-new-transactionscope-considered-harmful.aspx
		/// </summary>
		/// <returns></returns>
		protected static TransactionScope CreateTransactionScopeObject()
		{
			TransactionScope ts = new TransactionScope(
				TransactionScopeOption.RequiresNew,
				new TransactionOptions()
				{
					IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
					Timeout = TransactionManager.MaximumTimeout
				}
			);

			return ts;
		}




		/// <summary>
		/// Runs a provided SQL query and returns a DataTable of the results.
		/// </summary>
		/// <param name="sqlQuery"></param>
		/// <returns></returns>
		internal static DataTable GetDataTable(string sqlQuery)
		{
			DataTable data = new DataTable();

			// SqlConnection is provided "Open" so just use it.
			using (SqlConnection connection = GetSqlConnection(".\\sql2008express", "MANTA_MTA"))
			{
				SqlCommand command = connection.CreateCommand();
				command.CommandType = CommandType.Text;
				command.CommandText = sqlQuery;

				data.Load(command.ExecuteReader());

				connection.Close();
			}

			return data;
		}


		private static SqlConnection GetSqlConnection(string instance, string dbName)
		{
			SqlConnection connection = new SqlConnection(String.Format("server={0};database={1};trusted_connection=yes", instance, dbName));
			connection.Open();

			return connection;
		}
	}
}
