using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class ExchangeDeclareMiddleware : Pipe.Middleware.ExchangeDeclareMiddleware
	{
		public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider, ExchangeDeclareOptions options = null)
			: base(topologyProvider, options) { }

		protected override Task DeclareExchangeAsync(ExchangeDeclaration exchange, IPipeContext context, CancellationToken token)
		{
			// Polly 8.x: Updated to use ResiliencePipeline instead of IAsyncPolicy
			var pipeline = context.GetPolicy(PolicyKeys.ExchangeDeclare);
			return pipeline.ExecuteAsync(
				callback: async ct => await base.DeclareExchangeAsync(exchange, context, ct),
				cancellationToken: token).AsTask();
		}
	}
}
