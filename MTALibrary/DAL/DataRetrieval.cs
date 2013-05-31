using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Colony101.MTA.Library.DAL
{
	internal class DataRetrieval
	{
		/// <summary>
		/// Used for methods that create new Business Objects and fill them with data.
		/// </summary>
		/// <typeparam name="ObjectType">The Type of the Business Object, e.g. BroadcastEmail, Format,
		/// Audience, etc.</typeparam>
		/// <param name="record"></param>
		/// <returns></returns>
		internal delegate ObjectType CreateObjectMethod<ObjectType>(IDataRecord record);


		/// <summary>
		/// Used for methods that fill existing Business Objects with data.
		/// </summary>
		/// <typeparam name="ObjectType">The Type of the Business Object, e.g. BroadcastEmail, Format,
		/// Audience, etc.</typeparam>
		/// <param name="obj">An existing Business Object to fill with data from the database
		/// record provided by <paramref name="record"/>.</param>
		/// <param name="record">Database record containing values to copy to the Business Object
		/// provided by <paramref name="obj"/>.</param>
		/// <returns></returns>
		internal delegate void FillObjectMethod<ObjectType>(ObjectType obj, IDataRecord record);


		/// <summary>
		/// Attempts to retrieve a database record and return a Business Object populated with
		/// values from that record.
		/// </summary>
		/// <typeparam name="ObjectType">The Type of the Business Object that we're working with,
		/// e.g. BroadcastEmail, Format, Audience, etc.</typeparam>
		/// <param name="command">A SqlCommand to execute to retrieve a database record.</param>
		/// <param name="createObjectMethod">A delegate method used to create a Business Object
		/// and to copy values from the retrieved database record into it.</param>
		/// <returns>If a database record is not found by executing <paramref name="command"/>,
		/// null is returned.  If a database record does exist, an instantied object with
		/// values set from the retrieved database record is returned.</returns>
		internal static ObjectType GetSingleObjectFromDatabase<ObjectType>(SqlCommand command, CreateObjectMethod<ObjectType> createObjectMethod)
		{
			// BenC (2011-01-07): Set "obj" to null by calling default() with its type.
			// For reference types this'll be null, for value types it'll be zero.
			// So either null or an instantiated object with values from the database
			// will be what's returned by this our method.
			ObjectType obj = default(ObjectType);

			command.Connection.Open();

			using (SqlDataReader reader = command.ExecuteReader())
			{
				if (reader.Read())
				{
					obj = createObjectMethod(reader);
				}

				reader.Close();
			}


			command.Connection.Close();

			return obj;
		}


		/// <summary>
		/// Retrieves a database record and uses the FillObjectMethod delegate provided in
		/// <paramref name="fillObjectMethod"/> to copy values into an existing Business Object.
		/// </summary>
		/// <typeparam name="ObjectType">The Type of the Business Object, e.g. BroadcastEmail, Contact,
		/// Audience, etc.</typeparam>
		/// <param name="command">The SqlCommand object to execute that should return a database
		/// record.</param>
		/// <param name="obj">If a database record is found, this parameter will be populated by values
		/// from it and this method will return true.  If a database record is not found, this parameter
		/// will be untouched and this method will return false.</param>
		/// <param name="fillObjectMethod">A delegate method used to perform the copying of values
		/// from a retrieved database record into the object provided by <paramref name="obj"/>.</param>
		/// <returns>true if a database record was retrieved and used to fill the Business Object provided
		/// in <paramref name="obj"/>, else false.</returns>
		internal static bool FillSingleObjectFromDatabase<ObjectType>(SqlCommand command, ObjectType obj, FillObjectMethod<ObjectType> fillObjectMethod)
		{
			bool toReturn = false;

			command.Connection.Open();

			using (SqlDataReader reader = command.ExecuteReader())
			{
				if (reader.Read())
				{
					fillObjectMethod(obj, reader);

					toReturn = true;
				}

				reader.Close();
			}


			command.Connection.Close();

			return toReturn;
		}


		internal static List<ObjectType> GetCollectionFromDatabase<ObjectType>(SqlCommand command, CreateObjectMethod<ObjectType> createObjectMethod)
		{
			List<ObjectType> collection = new List<ObjectType>();

			command.Connection.Open();

			using (SqlDataReader reader = command.ExecuteReader())
			{
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						collection.Add(createObjectMethod(reader));
					}
				}

				reader.Close();
			}

			command.Connection.Close();

			return collection;
		}
	}

	/// <summary>
	/// Provides a set of extension methods for the IDataRecord class adding 
	/// support for calling the Get* methods with a column name as well.
	/// </summary>
	internal static class IDataRecordExtensions
	{
		/// <summary>
		/// Gets the value of the specified column as a Boolean.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static bool GetBoolean(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetBoolean(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as a byte.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static byte GetByte(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetByte(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Reads a stream of bytes from the specified column, starting at location indicated by <paramref name="dataOffset"/>, into the buffer, starting at the location indicated by <paramref name="bufferOffset"/>.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <param name="dataOffset">The index within the row from which to begin the read operation.</param>
		/// <param name="buffer">The buffer into which to copy the data.</param>
		/// <param name="bufferOffset">The index with the buffer to which the data will be copied.</param>
		/// <param name="length">The maximum number of characters to read.</param>
		/// <returns>The actual number of bytes read.</returns>
		public static long GetBytes(this IDataRecord myIDataRecord, string name, long dataOffset, byte[] buffer, int bufferOffset, int length)
		{
			return myIDataRecord.GetBytes(myIDataRecord.GetOrdinal(name), dataOffset, buffer, bufferOffset, length);
		}

		/// <summary>
		/// Gets the value of the specified column as a single character.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static char GetChar(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetChar(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Reads a stream of characters from the specified column, starting at the location indicated by <paramref name="dataOffset"/>, into the buffer, starting at the location indicated by <paramref name="bufferOffset"/>.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <param name="dataOffset">The index within the row from which to begin the read operation.</param>
		/// <param name="buffer">The buffer into which to copy the data.</param>
		/// <param name="bufferOffset">The index with the buffer to which the data will be copied.</param>
		/// <param name="length">The maximum number of characters to read.</param>
		/// <returns>The actual number of characters read.</returns>
		public static long GetChars(this IDataRecord myIDataRecord, string name, long dataOffset, char[] buffer, int bufferOffset, int length)
		{
			return myIDataRecord.GetChars(myIDataRecord.GetOrdinal(name), dataOffset, buffer, bufferOffset, length);
		}

		/// <summary>
		/// Gets name of the data type of the specified column.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>A string representing the name of the data type.</returns>
		public static string GetDataTypeName(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetDataTypeName(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as a <see cref="T:System.DateTime"/> object.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static DateTime GetDateTime(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetDateTime(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as a <see cref="T:System.Decimal"/> object.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static decimal GetDecimal(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetDecimal(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as a double-precision floating point number.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static double GetDouble(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetDouble(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the data type of the specified column.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The data type of the specified column.</returns>
		public static Type GetFieldType(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetFieldType(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as a single-precision floating point number.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static float GetFloat(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetFloat(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as a globally-unique identifier (GUID).
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static Guid GetGuid(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetGuid(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as a 16-bit signed integer.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static short GetInt16(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetInt16(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as a 32-bit signed integer.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static int GetInt32(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetInt32(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as a 64-bit signed integer.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static long GetInt64(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetInt64(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets the value of the specified column as an instance of <see cref="T:System.String"/>.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static string GetString(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetString(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Returns the string in the column if null returns string.empty
		/// </summary>
		/// <param name="record"></param>
		/// <param name="colName"></param>
		/// <returns></returns>
		public static string GetStringOrEmpty(this IDataRecord record, string name)
		{
			if (record.IsDBNull(name))
				return string.Empty;
			return record.GetString(name);
		}

		/// <summary>
		/// Gets the value of the specified column as an instance of <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>The value of the specified column.</returns>
		public static object GetValue(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.GetValue(myIDataRecord.GetOrdinal(name));
		}

		/// <summary>
		/// Gets a value that indicates whether the column contains nonexistent or missing values.
		/// </summary>
		/// <param name="myIDataRecord">The IDataRecord to extend.</param>
		/// <param name="name">The name of the column in the result set.</param>
		/// <returns>
		/// true if the specified column is equivalent to <see cref="T:System.DBNull"/>; otherwise false.
		/// </returns>
		public static bool IsDBNull(this IDataRecord myIDataRecord, string name)
		{
			return myIDataRecord.IsDBNull(myIDataRecord.GetOrdinal(name));
		}
	}
}
