using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Configuration;
using RawRabbit.Enrichers.Polly.Services;
using Xunit;

namespace RawRabbit.Enrichers.Polly.Tests.Services
{
	public class ChannelFactoryTests
	{
		[Fact]
		public async Task Should_Use_Connect_Policy_When_Connecting_To_Broker()
		{
			/* Setup */
			var connection = new Mock<IConnection>();
			var connectionFactory = new Mock<IConnectionFactory>();
			connectionFactory
				.Setup(f => f.CreateConnection())
				.Returns(connection.Object);
			connectionFactory
				.SetupSequence(c => c.CreateConnection(
						It.IsAny<List<string>>()
					))
				.Throws(new BrokerUnreachableException(new Exception()))
				.Throws(new BrokerUnreachableException(new Exception()))
				.Throws(new BrokerUnreachableException(new Exception()))
				.Returns(connection.Object);

			var pipeline = new ResiliencePipelineBuilder()
				.AddRetry(new RetryStrategyOptions
				{
					ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>(),
					MaxRetryAttempts = 5,
					Delay = TimeSpan.FromMilliseconds(1),
					BackoffType = DelayBackoffType.Exponential,
					UseJitter = false
				})
				.Build();

			var factory = new ChannelFactory(connectionFactory.Object, RawRabbitConfiguration.Local, new ConnectionPolicies{ Connect = pipeline });

			/* Test */
			/* Assert */
			await factory.ConnectAsync();
		}

		[Fact]
		public async Task Should_Use_Create_Channel_Policy_When_Creaing_Channels()
		{
			/* Setup */
			var channel = new Mock<IModel>();
			var connection = new Mock<IConnection>();
			var connectionFactory = new Mock<IConnectionFactory>();
			connectionFactory
				.Setup(f => f.CreateConnection())
				.Returns(connection.Object);
			connectionFactory
				.Setup(c => c.CreateConnection(
					It.IsAny<List<string>>()
				))
				.Returns(connection.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(true);
			connection
				.SetupSequence(c => c.CreateModel())
				.Throws(new TimeoutException())
				.Throws(new TimeoutException())
				.Returns(channel.Object);

			var pipeline = new ResiliencePipelineBuilder()
				.AddRetry(new RetryStrategyOptions
				{
					ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
					MaxRetryAttempts = 5,
					Delay = TimeSpan.FromMilliseconds(1),
					BackoffType = DelayBackoffType.Exponential,
					UseJitter = false
				})
				.Build();

			var factory = new ChannelFactory(connectionFactory.Object, RawRabbitConfiguration.Local, new ConnectionPolicies { CreateChannel = pipeline });

			/* Test */
			var retrievedChannel = await  factory.CreateChannelAsync();

			/* Assert */
			Assert.Equal(channel.Object, retrievedChannel);
		}
	}
}
