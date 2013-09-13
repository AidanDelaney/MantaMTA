using MantaMTA.Core.Message;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class HeaderTests : TestFixtureBase
	{
		/// <summary>
		/// Tests that adding a header to an email works. Checks that folding works when required.
		/// </summary>
		[Test]
		public void AddHeader()
		{
			string msg = @"Received: by mail-ve0-f170.google.com with SMTP id c14so7342138vea.29
        for <abuse@manta.snt0.net>; Thu, 12 Sep 2013 08:55:44 -0700 (PDT)
X-Google-DKIM-Signature: v=1; a=rsa-sha256; c=relaxed/relaxed;
        d=1e100.net; s=20130820;
        h=x-gm-message-state:mime-version:date:message-id:subject:from:to
         :content-type;
        bh=g3zLYH4xKxcPrHOD18z9YfpQcnk/GaJedfustWU5uGs=;
        b=QQp2spZaT8t1uP2qlmFDNRy5SMCkPfyAcwWAyTH8SbWtBMZ6hSssOHTdYEPb6aPbBQ
         qFquL9n20dtfSLU47+OLtB+cOoOtAt1FACRWN42x/V608OEAeu25KjTgnD3LFBvn482y
         PWmlpv8MlyulZvqT33dgp0C7Wb4HN5jGLgRajteAFfyiK+Hn0ouP+Q/WdWFm3SAiB4e2
         VVEPV+8hh79e3Bpwqjjnlu3xL8gG+kgqGKnzv7XsuSH7QdO7KDiVQLiWqvPv3je15eN5
         6fQW6hfnD9vU3oJtu4Qxnxw2LCSC8Ble2JVIqFuVlljwzVERv00b8GlpMUy4VoSv/oxZ
         jR9Q==
X-Gm-Message-State: ALoCoQkJ3V6JKWYCk5sVgPWozY51PLGpZk+oEFXvfjTeRXLdEwuyJt1ZZGIF8VY7cV9ZQkkKm5/U
MIME-Version: 1.0
X-Received: by 10.58.207.103 with SMTP id lv7mr1139399vec.33.1379000866633;
 Thu, 12 Sep 2013 08:47:46 -0700 (PDT)
Received: by 10.221.57.135 with HTTP; Thu, 12 Sep 2013 08:47:46 -0700 (PDT)
Date: Thu, 12 Sep 2013 16:47:46 +0100
Message-ID: <CAN1h+uc4QwREsHNftYp-8xqs-ZxHZ0-6qc06V_fekQsv+8NcEA@mail.gmail.com>
Subject: test
From: Redacted <redacted@colony101.co.uk>
To: abuse@manta.snt0.net
Content-Type: text/plain; charset=UTF-8

test";
			// Test with header value thats short enougth to not need folding.
			string returned = MessageManager.AddHeader(msg, new MessageHeader("Test", "test"));
			Assert.AreEqual("Test: test" + System.Environment.NewLine + msg, returned);

			// Test with header value that is longer than 78 but can't be folded as it contains no white space.
			returned = MessageManager.AddHeader(msg, new MessageHeader("Test", "testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest"));
			Assert.AreEqual(@"Test:
 testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest" + System.Environment.NewLine + msg, returned);
			
			// Test with long value that can be folded.
			returned = MessageManager.AddHeader(msg, new MessageHeader("Test", "test testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest"));
			Assert.AreEqual(@"Test: test
 testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest" + System.Environment.NewLine + msg, returned);
		}

		/// <summary>
		/// Tests removing of a header from an email message. Should no affect any other headers.
		/// </summary>
		[Test]
		public void RemoveHeader()
		{
			string message = @"Header1: Header1
Header2: Header2
Header3: Header 3
Header4: Header
 4
Header5: Header 5

Body!";
			string returned = MessageManager.RemoveHeader(message, "Header1");
			Assert.AreEqual( @"Header2: Header2
Header3: Header 3
Header4: Header
 4
Header5: Header 5

Body!", returned);

			returned = MessageManager.RemoveHeader(message, "Header2");
			Assert.AreEqual(@"Header1: Header1
Header3: Header 3
Header4: Header
 4
Header5: Header 5

Body!", returned);

			returned = MessageManager.RemoveHeader(message, "Header3");
			Assert.AreEqual(@"Header1: Header1
Header2: Header2
Header4: Header
 4
Header5: Header 5

Body!", returned);

			returned = MessageManager.RemoveHeader(message, "Header4");
			Assert.AreEqual(@"Header1: Header1
Header2: Header2
Header3: Header 3
Header5: Header 5

Body!", returned);

			returned = MessageManager.RemoveHeader(message, "Header5");
			Assert.AreEqual(@"Header1: Header1
Header2: Header2
Header3: Header 3
Header4: Header
 4

Body!", returned);
		}
	}
}
