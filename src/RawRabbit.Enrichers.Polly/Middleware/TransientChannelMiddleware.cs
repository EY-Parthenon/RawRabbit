using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class TransientChannelMiddleware : Pipe.Middleware.TransientChannelMiddleware
	{
		public TransientChannelMiddleware(IChannelFactory factory)
			: base(factory) { }

		protected override Task<IModel> CreateChannelAsync(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.ChannelCreate);
			var pollyContext = new Context
			{
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token,
				[RetryKey.ChannelFactory] = ChannelFactory
			};
			return policy.ExecuteAsync(
				action: (ctx, ct) => base.CreateChannelAsync(context, ct),
				context: pollyContext,
				cancellationToken: token
			);
		}
	}
}
