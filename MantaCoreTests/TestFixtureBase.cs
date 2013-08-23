using MantaMTA.Core.DAL;
using NUnit.Framework;
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
		/// Compares an enum to database records used to represent it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sqlQueryForEnumRecords"></param>
		/// <param name="enumIdColumn"></param>
		/// <param name="enumNameColumn"></param>
		protected void CompareEnumToDatabaseRecords<T>(string sqlQueryForEnumRecords, string enumIdColumn, string enumNameColumn)
		{
			DataTable table = GetDataTable(sqlQueryForEnumRecords);

			Assert.AreEqual(table.Rows.Count, Enum.GetValues(typeof(T)).Length, "The number of database records doesn't match the number of elements in the enum.");


			// Check each enum element has a matching record.
			foreach (var c in Enum.GetValues(typeof(T)))
			{
				bool foundRow = false;

				// Find the enum value's database record.
				foreach (DataRow r in table.Rows)
				{
					if ((int)r[enumIdColumn] == (int)c)
					{
						// Found the row.
						foundRow = true;

						Assert.AreEqual(c.ToString(), r[enumNameColumn].ToString());

						break;
					}
				}


				Assert.IsTrue(foundRow, "Failed to locate database record for enum value \"" + c + "\".");
			}
		}


		/// <summary>
		/// Runs a provided SQL query and returns a DataTable of the results.
		/// </summary>
		/// <param name="sqlQuery"></param>
		/// <returns></returns>
		protected static DataTable GetDataTable(string sqlQuery)
		{
			DataTable data = new DataTable();

			using (SqlConnection connection = MantaDB.GetSqlConnection())
			{
				SqlCommand command = connection.CreateCommand();
				command.CommandType = CommandType.Text;
				command.CommandText = sqlQuery;

				connection.Open();

				data.Load(command.ExecuteReader());

				connection.Close();
			}

			return data;
		}
	}
}
