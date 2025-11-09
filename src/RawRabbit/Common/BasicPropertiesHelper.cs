using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace RawRabbit.Common
{
	/// <summary>
	/// Helper class for creating IBasicProperties instances in RabbitMQ.Client 6.x
	/// where BasicProperties constructor is internal and requires using channel.CreateBasicProperties()
	/// </summary>
	public static class BasicPropertiesHelper
	{
		/// <summary>
		/// Creates a basic properties object using the provided channel.
		/// This is a lazy factory that will be invoked when a channel is available.
		/// </summary>
		public static Func<IModel, IBasicProperties> CreateBasicPropertiesFactory()
		{
			return channel => channel?.CreateBasicProperties();
		}

		/// <summary>
		/// Creates a basic properties object with default values using the provided channel.
		/// </summary>
		public static IBasicProperties CreateDefault(IModel channel, string contentType = "application/json", byte deliveryMode = 1)
		{
			if (channel == null)
			{
				return null;
			}

			var props = channel.CreateBasicProperties();
			props.MessageId = Guid.NewGuid().ToString();
			props.Headers = new Dictionary<string, object>();
			props.DeliveryMode = deliveryMode;
			props.ContentType = contentType;
			props.ContentEncoding = "UTF-8";
			return props;
		}
	}
}
