using System;
using System.Net.Mime;
using System.Text;

namespace MantaMTA.Core.Message
{
	/// <summary>
	/// A single part of a MimeMessage that contains part of the content.  This could be
	/// simply a container for other BodyParts or actual meaningful content as in plain text,
	/// HTML code, an image, etc.
	/// </summary>
	public class BodyPart
	{
		/// <summary>
		/// All the headers for this BodyPart.
		/// </summary>
		public MessageHeaderCollection Headers { get; set; }
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
		/// The Transfer encoded body - might not be legible without being decoded (so call
		/// GetDecodedBody() to have that appropriately decoded).
		/// </summary>
		public string EncodedBody { get; set; }
		/// <summary>
		/// A collection of MimeMessageBodyPart objects that make up the email.
		/// </summary>
		public BodyPart[] BodyParts{ get; set; }
		/// <summary>
		/// If true then this body part contains a MIME Message.
		/// </summary>
		public bool HasChildMimeMessage
		{
			get
			{
				if (ContentType == null || string.IsNullOrWhiteSpace(ContentType.MediaType))
					return false;

				return ContentType.MediaType.Equals("message/rfc822", StringComparison.OrdinalIgnoreCase);
			}
		}
		/// <summary>
		/// Returns a child MIME Message if one exists, else null.
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
		/// <summary>
		/// Get this Body Part decoded.
		/// </summary>
		/// <returns>Body part body with Transport Encoding decoded.</returns>
		public string GetDecodedBody()
		{
			string tmp = EncodedBody;

			if (TransferEncoding == TransferEncoding.Base64)
			{
				tmp = UTF8Encoding.Default.GetString(Convert.FromBase64String(tmp));
			}
			return tmp;
		}
	}
}
