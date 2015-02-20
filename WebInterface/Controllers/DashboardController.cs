using MantaMTA.Core;
using MantaMTA.Core.Enums;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebInterface.Models;

namespace WebInterface.Controllers
{
    public class DashboardController : Controller
    {
        //
        // GET: /Dashboard/
        public ActionResult Index()
        {
			DashboardModel model = new DashboardModel
			{
				SendTransactionSummaryCollection = WebInterfaceLib.DAL.TransactionDB.GetLastHourTransactionSummary(),
				Waiting = WebInterfaceLib.DAL.SendDB.GetQueueCount(new SendStatus[] { SendStatus.Active, SendStatus.Discard }),
				Paused = WebInterfaceLib.DAL.SendDB.GetQueueCount(new SendStatus[] { SendStatus.Paused }),
				BounceInfo = WebInterfaceLib.DAL.TransactionDB.GetLastHourBounceInfo(3),
				SendSpeedInfo = WebInterfaceLib.DAL.TransactionDB.GetLastHourSendSpeedInfo()
			};

			// Connect to Rabbit MQ and grab basic queue counts.
			HttpWebRequest request = HttpWebRequest.CreateHttp("http://localhost:15672/api/queues");
			request.Credentials = new NetworkCredential(MtaParameters.RabbitMQ.Username, MtaParameters.RabbitMQ.Password);
			using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			{
				
				string json = new StreamReader(response.GetResponseStream()).ReadToEnd();
				JArray rabbitQueues = JArray.Parse(json);
				foreach(JToken q in rabbitQueues.Children())
				{
					JEnumerable<JProperty> qProperties = q.Children<JProperty>();
					string queueName = (string)qProperties.First(x => x.Name.Equals("name")).Value;
					if(queueName.StartsWith("manta_mta_"))
					{
						long messages = (long)qProperties.First(x => x.Name.Equals("messages", System.StringComparison.OrdinalIgnoreCase)).Value;
						if (queueName.IndexOf("_inbound_") > 0)
							model.RabbitMqInbound += messages;
						else if (queueName.IndexOf("_outbound_") > 0)
							model.RabbitMqTotalOutbound += messages;
					}
				}
			}


			return View(model);
        }
    }
}
