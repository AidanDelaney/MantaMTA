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
	}
}
