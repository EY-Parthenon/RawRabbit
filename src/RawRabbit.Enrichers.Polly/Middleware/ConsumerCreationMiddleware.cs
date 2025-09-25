using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
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

		protected override Task<IBasicConsumer> GetOrCreateConsumerAsync(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.QueueDeclare);
			var pollyContext = new Context
			{
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token,
				[RetryKey.ConsumerFactory] = ConsumerFactory,
			};
			return policy.ExecuteAsync(
				action: (ctx, ct) => base.GetOrCreateConsumerAsync(context, ct),
				context: pollyContext,
				cancellationToken: token
			);
		}
	}
}
