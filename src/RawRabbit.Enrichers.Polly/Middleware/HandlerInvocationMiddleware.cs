using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class HandlerInvocationMiddleware : Pipe.Middleware.HandlerInvocationMiddleware
	{
		public HandlerInvocationMiddleware(HandlerInvocationOptions options = null)
			: base(options) { }

		protected override async Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.HandlerInvocation);
			await policy.ExecuteAsync(
				async ct => await base.InvokeMessageHandler(context, ct),
				token);
		}
	}
}
