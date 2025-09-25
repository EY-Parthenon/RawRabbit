using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
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
			var policy = context.GetPolicy(PolicyKeys.ExchangeDeclare);
			var pollyContext = new Context
			{
				[RetryKey.TopologyProvider] = TopologyProvider,
				[RetryKey.ExchangeDeclaration] = exchange,
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token,
			};
			return policy.ExecuteAsync(
				action: (ctx, ct) => base.DeclareExchangeAsync(exchange, context, ct),
				context: pollyContext,
				cancellationToken: token);
		}
	}
}
