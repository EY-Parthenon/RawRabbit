using System;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher
{
	public class PublisherConfigurationFactory : IPublisherConfigurationFactory
	{
		private readonly IExchangeDeclarationFactory _exchange;
		private readonly IBasicPublishConfigurationFactory _basicPublish;

		public PublisherConfigurationFactory(IExchangeDeclarationFactory exchange, IBasicPublishConfigurationFactory basicPublish)
		{
			_exchange = exchange;
			_basicPublish = basicPublish;
		}

		public PublisherConfiguration Create<TMessage>()
		{
			return Create(typeof(TMessage));
		}

		public PublisherConfiguration Create(Type messageType)
		{
			var cfg = _basicPublish.Create(messageType);
			return new PublisherConfiguration
			{
				BasicProperties = cfg.BasicProperties,
				Body = cfg.Body,
				Exchange = _exchange.Create(messageType),
				ExchangeName = cfg.ExchangeName,
				Mandatory = cfg.Mandatory,
				RoutingKey = cfg.RoutingKey
			};
		}

		public PublisherConfiguration Create(string exchangeName, string routingKey)
		{
			return new PublisherConfiguration
			{
				Exchange = _exchange.Create(exchangeName),
				ExchangeName = exchangeName,
				RoutingKey = routingKey,
				// RabbitMQ.Client 6.x: BasicProperties is internal, use SimpleBasicProperties instead
			BasicProperties = new SimpleBasicProperties()
			};
		}
	}
}