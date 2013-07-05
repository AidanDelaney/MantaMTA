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
		/// <param name="smtpCode">A standard SMTP code.</param>
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
					return MantaBounceCode.RejectedUnknown;


				default:
					// Do additional processing if no actual matches above.
					if (smtpCode.ToString()[0] == '4')
						return MantaBounceCode.DeferredGeneral;
					else if (smtpCode.ToString()[0] == '5')
						return MantaBounceCode.RejectedUnknown;

					// The final catchall.
					return MantaBounceCode.BounceUnknown;
			}			
		}
	}
}