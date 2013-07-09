using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Mvc;

namespace WebInterface.Controllers
{
    public class StatsController : Controller
    {
        //
        // GET: /StatsController/

        public ActionResult Index()
        {
			List<TcpConnectionInformation> smtpConns = new List<TcpConnectionInformation>(IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Where(tcp => tcp.RemoteEndPoint.Port == 25 || tcp.LocalEndPoint.Port == 25));

			return View(new List<TcpConnectionInformation>[]
			{
				new List<TcpConnectionInformation>(smtpConns.Where(tcp => tcp.LocalEndPoint.Port == 25)),
				new List<TcpConnectionInformation>(smtpConns.Where(tcp => tcp.RemoteEndPoint.Port == 25))
			});
        }

    }
}
