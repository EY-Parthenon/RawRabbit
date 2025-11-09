using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
			// Polly 8.x: Updated to use ResiliencePipeline instead of IAsyncPolicy
			var pipeline = context.GetPolicy(PolicyKeys.QueueBind);
			return pipeline.ExecuteAsync(
				callback: async ct => await base.BindQueueAsync(queue, exchange, routingKey, context, ct),
				cancellationToken: token
			).AsTask();
		}
	}
}
