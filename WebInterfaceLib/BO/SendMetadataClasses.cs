using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebInterfaceLib.BO
{
	public class SendMetadata
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

	public class SendMetadataCollection : List<SendMetadata>
	{
		public SendMetadataCollection() { }
		public SendMetadataCollection(IEnumerable<SendMetadata> collection) : base(collection) { }
	}
}
