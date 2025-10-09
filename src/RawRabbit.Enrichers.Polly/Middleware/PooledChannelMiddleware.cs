using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class PooledChannelMiddleware : Pipe.Middleware.PooledChannelMiddleware
	{
		public PooledChannelMiddleware(IChannelPoolFactory poolFactory, PooledChannelOptions options = null)
			: base(poolFactory, options) { }

		protected override async Task<IModel> GetChannelAsync(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.ChannelCreate);
			return await policy.ExecuteAsync(
				async ct => await base.GetChannelAsync(context, ct),
				token);
		}
	}
}
