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

		protected override async Task BindQueueAsync(string queue, string exchange, string routingKey, IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.QueueBind);
			await policy.ExecuteAsync(
				async ct => await base.BindQueueAsync(queue, exchange, routingKey, context, ct),
				token);
		}
	}
}
