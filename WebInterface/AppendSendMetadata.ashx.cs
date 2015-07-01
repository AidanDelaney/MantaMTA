using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebInterface
{
	/// <summary>
	/// Summary description for AppendSendMetadata
	/// </summary>
	public class AppendSendMetadata : IHttpHandler
	{

		public void ProcessRequest(HttpContext context)
		{
			context.Response.ContentType = "text/plain";

			string[] relayingIPs = MantaMTA.Core.MtaParameters.IPsToAllowRelaying;
			if(!relayingIPs.Contains(context.Request.UserHostAddress))
			{
				context.Response.Write("Forbidden");
				return;
			}


			string sendID = context.Request.QueryString["SendID"];
			string name = context.Request.QueryString["Name"];
			string value = context.Request.QueryString["Value"];

			if (string.IsNullOrWhiteSpace(sendID) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
			{
				context.Response.Write("bad");
				return;
			}

			MantaMTA.Core.Sends.Send snd = MantaMTA.Core.Sends.SendManager.Instance.GetSendAsync(sendID).Result;
			WebInterfaceLib.DAL.SendDB.SaveSendMetadata(snd.InternalID, new WebInterfaceLib.BO.SendMetadata { 
				Name = name,
				Value = value
			});

			
			context.Response.Write("ok");
		}

		public bool IsReusable
		{
			get
			{
				return true;
			}
		}
	}
}