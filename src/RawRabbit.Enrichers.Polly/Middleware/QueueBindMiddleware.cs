using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class QueueBindMiddleware : Pipe.Middleware.QueueBindMiddleware
	{
		public QueueBindMiddleware(ITopologyProvider topologyProvider, QueueBindOptions options = null)
			: base(topologyProvider, options) { }

		protected override Task BindQueueAsync(string queue, string exchange, string routingKey, IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.QueueBind);
			var pollyContext = new Context
			{
				[RetryKey.TopologyProvider] = TopologyProvider,
				[RetryKey.QueueName] = queue,
				[RetryKey.ExchangeName] = exchange,
				[RetryKey.RoutingKey] = routingKey,
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token
			};
			return policy.ExecuteAsync(
				action: (ctx, ct) => base.BindQueueAsync(queue, exchange, routingKey, context, ct),
				context: pollyContext,
				cancellationToken: token
			);
		}
	}
}
