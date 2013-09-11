using System.Collections.Generic;

namespace MantaMTA.Core
{
	/// <summary>
	/// Holds a local domain object.
	/// </summary>
	public class LocalDomain
	{
		/// <summary>
		/// ID of the local domain.
		/// </summary>
		public int ID { get; set; }
		
		/// <summary>
		/// Local domains hostname.
		/// </summary>
		public string Hostname { get; set; }

		/// <summary>
		/// Name for the local domain
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Description of the local domain.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Constuctor sets defaults.
		/// </summary>
		public LocalDomain()
		{
			this.Description = string.Empty;
			this.Hostname = string.Empty;
			this.ID = 0;
			this.Name = string.Empty;
		}
	}

	/// <summary>
	/// Holds a collection of localdomains.
	/// </summary>
	public class LocalDomainCollection : List<LocalDomain>
	{
		public LocalDomainCollection() { }
		public LocalDomainCollection(IEnumerable<LocalDomain> collection) : base(collection) { }
	}
}
