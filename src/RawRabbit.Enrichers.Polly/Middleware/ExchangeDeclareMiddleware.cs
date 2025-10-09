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

		protected override async Task DeclareExchangeAsync(ExchangeDeclaration exchange, IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.ExchangeDeclare);
			await policy.ExecuteAsync(
				async ct => await base.DeclareExchangeAsync(exchange, context, ct),
				token);
		}
	}
}
