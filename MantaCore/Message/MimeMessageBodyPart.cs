using System;
using System.Net.Mime;
using System.Text;

namespace MantaMTA.Core.Message
{
	public class MimeMessageBodyPart
	{
		/// <summary>
		/// Holds the boundary
		/// </summary>
		public string Boundary { get; set; }
		/// <summary>
		/// The content type of the body part.
		/// </summary>
		public ContentType ContentType { get; set; }
		/// <summary>
		/// The Transfer encoding for the body part.
		/// </summary>
		public TransferEncoding TransferEncoding { get; set; }
		/// <summary>
		/// The Transfer encoded body
		/// </summary>
		public string EncodedBody { get; set; }
		public MimeMessageBodyPart[] BodyParts{ get; set; }

		/// <summary>
		/// If true then this body part contains a MIME Message
		/// </summary>
		public bool HasChildMimeMessage
		{
			get
			{
				if (ContentType == null)
					return false;

				return ContentType.MediaType.Equals("message/rfc822", StringComparison.OrdinalIgnoreCase);
			}
		}

		/// <summary>
		/// Child message or null
		/// </summary>
		public MimeMessage ChildMimeMessage
		{
			get
			{
				if (!HasChildMimeMessage)
					return null;

				return MimeMessage.Parse(this.GetDecodedBody());
			}
		}

		public MimeMessageBodyPart()
		{
			TransferEncoding = TransferEncoding.Unknown;
		}

		/// <summary>
		/// Get this Body Part decoded.
		/// </summary>
		/// <returns>Body part body with Transport Encoding decoded.</returns>
		public string GetDecodedBody()
		{
			string tmp = EncodedBody.Trim();
			if (TransferEncoding == TransferEncoding.Base64)
			{
				tmp = UTF8Encoding.Default.GetString(Convert.FromBase64String(tmp));
			}
			return tmp;
		}
	}
}
