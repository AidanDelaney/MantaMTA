using System;
using System.ServiceModel;

namespace MantaMTA.Core.ServiceContracts
{
	/// <summary>
	/// Make working with WCF Services nicer.
	/// </summary>
	public static class ServiceContractManager
	{
		/// <summary>
		/// Holds the send point addresses of contracts.
		/// </summary>
		public struct ServiceAddresses
		{
			/// <summary>
			/// The send manager endpoint.
			/// </summary>
			public const string SendManager = "SendManager";
		}

		/// <summary>
		/// Base endpoint address.
		/// </summary>
		private const string _baseAddress = "net.pipe://localhost/MantaMTA";

		/// <summary>
		/// Creates a ServiceHost using the specified parameters.
		/// </summary>
		/// <param name="serviceType">Type that implements contact.</param>
		/// <param name="implementedContract">Type of the contact that is implemented</param>
		/// <param name="address">Endpoint address.</param>
		/// <param name="faultedAction">EventHandler for service host Faulted</param>
		/// <returns>ServiceHost</returns>
		public static ServiceHost CreateServiceHost(Type serviceType, Type implementedContract, string address, EventHandler faultedAction)
		{
			// Create the service host.
			ServiceHost serviceHost = new ServiceHost(
				serviceType,
				new Uri[] { new Uri(_baseAddress) }
				);

			// Attatch it to an endpoint.
			serviceHost.AddServiceEndpoint(implementedContract, new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), address);

			// Add the faulted event handler.
			serviceHost.Faulted += faultedAction;

			// Return the service.
			return serviceHost;
		}

		/// <summary>
		/// Get a service channel.
		/// </summary>
		/// <typeparam name="T">Typeof the contact to get channel for.</typeparam>
		/// <returns>Service Contract.</returns>
		public static T GetServiceChannel<T>()
		{
			// Workout the endpoint address needed for contact.
			string serviceAddress = string.Empty;

			// If couldn't workout the endpoint then contact isn't implemented.
			if (string.IsNullOrWhiteSpace(serviceAddress))
				throw new NotImplementedException();

			// Create the ChannelFactory
			ChannelFactory<T> pipeFactory = new ChannelFactory<T>(new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), new EndpointAddress(_baseAddress + "/" + serviceAddress));
			// And finally get the channel to the service contract.
			return pipeFactory.CreateChannel();
		}
	}
}
