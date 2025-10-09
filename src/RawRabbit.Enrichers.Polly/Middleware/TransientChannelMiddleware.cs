using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class TransientChannelMiddleware : Pipe.Middleware.TransientChannelMiddleware
	{
		public TransientChannelMiddleware(IChannelFactory factory)
			: base(factory) { }

		protected override async Task<IModel> CreateChannelAsync(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.ChannelCreate);
			return await policy.ExecuteAsync(
				async ct => await base.CreateChannelAsync(context, ct),
				token);
		}
	}
}
