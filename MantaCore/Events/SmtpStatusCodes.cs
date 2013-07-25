using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MantaMTA.Core.Events
{
	internal partial class BounceRulesManager
	{
		/// <summary>
		/// Converts an SMTP status code into a MantaBounceType and MantaBounceCode pairing.
		/// </summary>
		/// <param name="smtpCode">A standard SMTP code, e.g. "550".</param>
		/// <returns>The appropriate MantaBounceCode for the SMTP code provided in <paramref name="smtpCode"/>.</returns>
		internal BouncePair ConvertSmtpCodeToMantaBouncePair(int smtpCode)
		{
			BouncePair bp = new BouncePair();


			// Based on the first character of the SMTP code, we can tell if it's not actually a bounce
			// or at least get the BounceType (whether it's a permanent or temporary problem).
			char codeClass = smtpCode.ToString()[0];

			if (codeClass == '2' || codeClass == '3')
			{
				// All done - not a bounce.

				bp.BounceType = MantaBounceType.Unknown;
				bp.BounceCode = MantaBounceCode.NotABounce;

				return bp;
			}
			else if (codeClass == '4')
				// Temporary problem.
				bp.BounceType = MantaBounceType.Soft;

			else if (codeClass == '5')
				// Permanent problem.
				bp.BounceType = MantaBounceType.Hard;
			else
			{
				// Perhaps not a valid SMTP code at all.
				bp.BounceType = MantaBounceType.Unknown;
				bp.BounceCode = MantaBounceCode.Unknown;

				return bp;
			}



			switch (smtpCode)
			{
				case 200://	(nonstandard success response, see rfc876)
				case 211://	System status, or system help reply
				case 214://	Help message
				case 220://	SMTP Service ready.
				case 221://	Service closing transmission channel
				case 250://	Requested mail action okay, completed
				case 251://	The recipient is not local to the server, but the server will accept and forward the message.
				case 252://	The recipient cannot be VRFYed, but the server accepts the message and attempts delivery.
				case 354://	Start mail input; end with <CRLF>.<CRLF>
					bp.BounceType = MantaBounceType.Unknown;
					bp.BounceCode = MantaBounceCode.NotABounce;
					return bp;

				case 420:// Timeout communication problem encountered during transmission
				case 421://	Service not available, closing transmission channel				
				case 521://	<domain> does not accept mail (see rfc1846)
				case 530://	Access denied (???a Sendmailism)
					bp.BounceCode = MantaBounceCode.ServiceUnavailable;
					break;

				case 431:// Receiving mail server's disk is full
				case 452://	Requested action not taken: insufficient system storage
				case 552://	Requested mail action aborted: exceeded storage allocation
					bp.BounceCode = MantaBounceCode.MailboxFull;
					break;

				case 450://	Requested mail action not taken: mailbox unavailable
				case 550://	Requested action not taken: mailbox unavailable
				case 551://	User not local; please try <forward-path>
				case 553://	Requested action not taken: mailbox name not allowed
					bp.BounceCode = MantaBounceCode.BadEmailAddress;
					break;

				case 571:// Message refused.
					bp.BounceCode = MantaBounceCode.RelayDenied;
					break;

				case 451://	Requested action aborted: local error in processing
				case 500://	Syntax error, command unrecognised
				case 501://	Syntax error in parameters or arguments
				case 502://	Command not implemented
				case 503://	Bad sequence of commands
				case 504://	Command parameter not implemented
				case 554://	Transaction failed
					bp.BounceCode = MantaBounceCode.General;
					break;

				default:
					bp.BounceCode = MantaBounceCode.Unknown;
					break;
			}

			return bp;
		}


		/// <summary>
		/// Converts a Non-Delivery Report (NDR) code to a MantaBounceType and MantaBounceCode.
		/// </summary>
		/// <param name="smtpCode">An NDR code, e.g. "4.4.7".  See here for more:
		/// http://tools.ietf.org/html/rfc3463.</param>
		/// <returns>A BouncePair object with the appropriate MantaBounceCode and MantaBounceType values
		/// for the NDR code provided in <paramref name="ndrCode"/>.</returns>
		internal BouncePair ConvertNdrCodeToMantaBouncePair(string ndrCode)
		{
			BouncePair bp = new BouncePair();


			int firstDotPos = ndrCode.IndexOf('.');

			// If it ain't got no dots, it ain't a proper NDR code.
			if (firstDotPos == -1)
			{
				bp.BounceType = MantaBounceType.Unknown;
				bp.BounceCode = MantaBounceCode.Unknown;

				return bp;
			}


			// Identify if it's a temporary or permanent bounce (or even not one at all).
			if (ndrCode.StartsWith("2") || ndrCode.StartsWith("3"))
			{
				// All done - not a bounce.

				bp.BounceType = MantaBounceType.Unknown;
				bp.BounceCode = MantaBounceCode.NotABounce;

				return bp;
			}
			if (ndrCode.StartsWith("4."))
				bp.BounceType = MantaBounceType.Soft;
			else if (ndrCode.StartsWith("5."))
				bp.BounceType = MantaBounceType.Hard;
			else
			{
				bp.BounceType = MantaBounceType.Unknown;
				bp.BounceCode = MantaBounceCode.Unknown;

				return bp;
			}




			
			// Check the rest of the code.
			string endPart = ndrCode.Substring(firstDotPos);



			// TODO BenC (2013-07-08): Needs refining/reviewing as just did a rough first pass through.
			switch (endPart)
			{
				case ".1.5":	// Destination mailbox address valid
					bp.BounceCode = MantaBounceCode.NotABounce;
					break;

				case ".1.0":	// Other address status
				case ".1.1":	// Bad destination mailbox address
				case ".1.2":	// Bad destination system address
				case ".1.3":	// Bad destination mailbox address syntax
				case ".1.4":	// Destination mailbox address ambiguous
				case ".1.6":	// Mailbox has moved
				case ".2.0":	// Other or undefined mailbox status
				case ".2.1":	// Mailbox disabled, not accepting messages
					bp.BounceCode = MantaBounceCode.BadEmailAddress;
					break;					

				case ".2.2":	// Mailbox full
				case ".3.1":	// Mail system full
					bp.BounceCode = MantaBounceCode.MailboxFull;
					break;

				case ".2.3":	// Message length exceeds administrative limit.
				case ".3.4":	// Message too big for system
					bp.BounceCode = MantaBounceCode.MessageSizeTooLarge;
					break;

				case ".2.4":	// Mailing list expansion problem
				case ".3.0":	// Other or undefined mail system status
				case ".3.2":	// System not accepting network messages
				case ".3.3":	// System not capable of selected features
				case ".4.0":	// Other or undefined network or routing status
				case ".4.1":	// No answer from host
				case ".4.2":	// Bad connection
				case ".4.3":	// Routing server failure
				case ".4.4":	// Unable to route
				case ".4.5":	// Network congestion
				case ".4.6":	// Routing loop detected
				case ".4.7":	// Delivery time expired
				case ".5.0":	// Other or undefined protocol status
					bp.BounceCode = MantaBounceCode.UnableToConnect;
					break;

				/*case ".1.7":	// Bad sender's mailbox address syntax
				case ".1.8":	// Bad sender's system address
				case ".5.1":	// Invalid command
				case ".5.2":	// Syntax error
				case ".5.3":	// Too many recipients
				case ".5.4":	// Invalid command arguments
				case ".5.5":	// Wrong protocol version
				case ".6.0":	// Other or undefined media error
				case ".6.1":	// Media not supported
				case ".6.2":	// Conversion required and prohibited
				case ".6.3":	// Conversion required but not supported
				case ".6.4":	// Conversion with loss performed
				case ".6.5":	// Conversion failed
				case ".7.0":	// Other or undefined security status
				case ".7.1":	// Delivery not authorized, message refused
				case ".7.2":	// Mailing list expansion prohibited
				case ".7.3":	// Security conversion required but not possible
				case ".7.4":	// Security features not supported
				case ".7.5":	// Cryptographic failure
				case ".7.6":	// Cryptographic algorithm not supported
				case ".7.7":	// Message integrity failure*/
				default:
					// Do additional processing if no matches above.
					bp.BounceCode = MantaBounceCode.General;
					break;
			}

			return bp;
		}
	}
}