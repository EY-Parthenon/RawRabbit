using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Consumer;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class ConsumerCreationMiddleware : Pipe.Middleware.ConsumerCreationMiddleware
	{
		public ConsumerCreationMiddleware(IConsumerFactory consumerFactory, ConsumerCreationOptions options = null)
			: base(consumerFactory, options) { }

		protected override async Task<IBasicConsumer> GetOrCreateConsumerAsync(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.QueueDeclare);
			return await policy.ExecuteAsync(
				async ct => await base.GetOrCreateConsumerAsync(context, ct),
				token);
		}
	}
}
