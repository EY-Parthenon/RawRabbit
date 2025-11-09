using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;

namespace RawRabbit.Configuration.BasicPublish
{
	public class BasicPublishConfigurationBuilder : IBasicPublishConfigurationBuilder
	{
		public BasicPublishConfiguration Configuration { get; }

		public BasicPublishConfigurationBuilder(BasicPublishConfiguration initial)
		{
			Configuration = initial;
		}

		public IBasicPublishConfigurationBuilder OnExchange(string exchange)
		{
			Configuration.ExchangeName = exchange;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}

		public IBasicPublishConfigurationBuilder AsMandatory(bool mandatory = true)
		{
			Configuration.Mandatory = mandatory;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithProperties(Action<IBasicProperties> propAction)
		{
			// RabbitMQ.Client 6.x: BasicProperties is internal, use SimpleBasicProperties instead
		Configuration.BasicProperties = Configuration.BasicProperties ?? new SimpleBasicProperties();
			propAction?.Invoke(Configuration.BasicProperties);
			return this;
		}
	}
}