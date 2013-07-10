using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MantaMTA.Core.Events
{
	internal partial class BounceRulesManager
	{
		/// <summary>
		/// Converts an SMTP status code into a MantaBounceCode.
		/// </summary>
		/// <param name="smtpCode">A standard SMTP code, e.g. "550".</param>
		/// <returns>The appropriate MantaBounceCode for the SMTP code provided in <paramref name="smtpCode"/>.</returns>
		internal MantaBounceCode ConvertSmtpCodeToMantaBounceCode(int smtpCode)
		{
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
					return MantaBounceCode.NotABounce;

				case 420:// Timeout communication problem encountered during transmission
				case 421://	Service not available, closing transmission channel
					return MantaBounceCode.DeferredUnableToConnect;

				case 431:// Receiving mail server's disk is full
					return MantaBounceCode.DeferredMailboxFull;

				case 450://	Requested mail action not taken: mailbox unavailable
					return MantaBounceCode.DeferredBadEmailAddress;

				case 451://	Requested action aborted: local error in processing
					return MantaBounceCode.DeferredUnableToConnect;

				case 452://	Requested action not taken: insufficient system storage
					return MantaBounceCode.DeferredMailboxFull;

				case 500://	Syntax error, command unrecognised
				case 501://	Syntax error in parameters or arguments
				case 502://	Command not implemented
				case 503://	Bad sequence of commands
				case 504://	Command parameter not implemented
				case 521://	<domain> does not accept mail (see rfc1846)
				case 530://	Access denied (???a Sendmailism)
					return MantaBounceCode.RejectedUnableToConnect;

				case 550://	Requested action not taken: mailbox unavailable
				case 551://	User not local; please try <forward-path>
					return MantaBounceCode.RejectedBadEmailAddress;

				case 552://	Requested mail action aborted: exceeded storage allocation
					return MantaBounceCode.RejectedMailboxFull;

				case 553://	Requested action not taken: mailbox name not allowed
					return MantaBounceCode.RejectedBadEmailAddress;

				case 554://	Transaction failed
					return MantaBounceCode.RejectedGeneral;


				default:
					// Do additional processing if no matches above.

					char codeClass = smtpCode.ToString()[0];

					if (codeClass == '2' || codeClass == '3')
						return MantaBounceCode.NotABounce;
					else if (codeClass == '4')
						return MantaBounceCode.DeferredGeneral;
					else if (codeClass == '5')
						return MantaBounceCode.RejectedGeneral;

					// The final catchall.
					return MantaBounceCode.Unknown;
			}			
		}


		/// <summary>
		/// Converts a Non-Delivery Report (NDR) code to a MantaBounceCode.
		/// </summary>
		/// <param name="smtpCode">An NDR code, e.g. "4.4.7".  See here for more:
		/// http://tools.ietf.org/html/rfc3463.</param>
		/// <returns>The appropriate MantaBounceCode for the NDR code provided in <paramref name="ndrCode"/>.</returns>
		internal MantaBounceCode ConvertNdrCodeToMantaBounceCode(string ndrCode)
		{
			bool isPermanentError = false;
			int firstDotPos = ndrCode.IndexOf('.');

			// If it ain't got no dots, it ain't a proper NDR code.
			if (firstDotPos == -1)
				return MantaBounceCode.Unknown;


			// Identify if it's a permanent or temporary bounce (or even not one at all).
			if (ndrCode.StartsWith("4."))
				isPermanentError = false;
			else if (ndrCode.StartsWith("5."))
				isPermanentError = true;
			else
				return MantaBounceCode.NotABounce;
			
			// Check the rest of the code.
			string endPart = ndrCode.Substring(firstDotPos);



			// TODO BenC (2013-07-08): Needs refiniing/reviewing as just did a rough pass through.
			switch (endPart)
			{
				case ".1.5":	// Destination mailbox address valid
					return MantaBounceCode.NotABounce;

				case ".1.0":	// Other address status
				case ".1.1":	// Bad destination mailbox address
				case ".1.2":	// Bad destination system address
				case ".1.3":	// Bad destination mailbox address syntax
				case ".1.4":	// Destination mailbox address ambiguous
				case ".1.6":	// Mailbox has moved
				case ".2.0":	// Other or undefined mailbox status
				case ".2.1":	// Mailbox disabled, not accepting messages
					return (isPermanentError ? MantaBounceCode.RejectedBadEmailAddress : MantaBounceCode.DeferredBadEmailAddress);

					

				case ".2.2":	// Mailbox full
				case ".3.1":	// Mail system full
					return (isPermanentError ? MantaBounceCode.RejectedMailboxFull: MantaBounceCode.DeferredMailboxFull);

				case ".2.3":	// Message length exceeds administrative limit.
				case ".3.4":	// Message too big for system
					return (isPermanentError ? MantaBounceCode.RejectedMessageSizeTooLarge : MantaBounceCode.DeferredMessageSizeTooLarge);

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
					return (isPermanentError ? MantaBounceCode.RejectedUnableToConnect : MantaBounceCode.DeferredUnableToConnect);



				case ".1.7":	// Bad sender's mailbox address syntax
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
				case ".7.7":	// Message integrity failure
				default:
					// Do additional processing if no matches above.
					return (isPermanentError ? MantaBounceCode.RejectedGeneral : MantaBounceCode.DeferredGeneral);
			}
		}
	}
}