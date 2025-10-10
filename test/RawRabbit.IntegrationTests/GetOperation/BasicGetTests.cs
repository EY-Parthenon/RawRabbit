using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.GetOperation
{
	public class BasicGetTests : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Be_Able_To_Get_Message()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage {Prop = "Get me, get it?"};
				var conventions = new NamingConventions();
				// Use unique queue and exchange names to avoid PRECONDITION_FAILED errors from state conflicts between test runs
				var testId = System.Guid.NewGuid().ToString();
				var queueName = $"{conventions.QueueNamingConvention(message.GetType())}-{testId}";
				var exchangeName = $"{conventions.ExchangeNamingConvention(message.GetType())}-{testId}";
				TestChannel.QueueDeclare(queueName, true, false, false, null);
				TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
				TestChannel.QueueBind(queueName, exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				var ackable = await client.GetAsync<BasicMessage>(cfg => cfg.FromQueue(queueName));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Prop, message.Prop);
				TestChannel.QueueDelete(queueName);
				TestChannel.ExchangeDelete(exchangeName);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Get_BasicGetResult_Message()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage { Prop = "Get me, get it?" };
				var conventions = new NamingConventions();
				// Use unique queue and exchange names to avoid PRECONDITION_FAILED errors from state conflicts between test runs
				var testId = System.Guid.NewGuid().ToString();
				var queueName = $"{conventions.QueueNamingConvention(message.GetType())}-{testId}";
				var exchangeName = $"{conventions.ExchangeNamingConvention(message.GetType())}-{testId}";
				TestChannel.QueueDeclare(queueName, true, false, false, null);
				TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
				TestChannel.QueueBind(queueName, exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				var ackable = await client.GetAsync(cfg => cfg.FromQueue(queueName));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.NotEmpty(ackable.Content.Body);
				TestChannel.QueueDelete(queueName);
				TestChannel.ExchangeDelete(exchangeName);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Get_BasicGetResult_When_Queue_IsEmpty()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage();
				var conventions = new NamingConventions();
				// Use unique queue name to avoid PRECONDITION_FAILED errors from state conflicts between test runs
				var queueName = $"{conventions.QueueNamingConvention(message.GetType())}-{System.Guid.NewGuid()}";
				TestChannel.QueueDeclare(queueName, true, false, false, null);

				/* Test */
				var ackable = await client.GetAsync(cfg => cfg.FromQueue(queueName));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Null(ackable.Content);
				TestChannel.QueueDelete(queueName);
			}
		}
	}
}
