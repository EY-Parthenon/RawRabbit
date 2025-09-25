using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class HandlerInvocationMiddleware : Pipe.Middleware.HandlerInvocationMiddleware
	{
		public HandlerInvocationMiddleware(HandlerInvocationOptions options = null)
			: base(options) { }

		protected override Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.HandlerInvocation);
			var pollyContext = new Context
			{
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token
			};
			return policy.ExecuteAsync(
				action: (ctx, ct) => base.InvokeMessageHandler(context, ct),
				context: pollyContext,
				cancellationToken: token);
		}
	}
}
