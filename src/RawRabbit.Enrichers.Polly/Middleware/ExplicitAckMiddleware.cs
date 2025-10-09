using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class ExplicitAckMiddleware : Pipe.Middleware.ExplicitAckMiddleware
	{
		public ExplicitAckMiddleware(INamingConventions conventions, ITopologyProvider topology, IChannelFactory channelFactory, ExplicitAckOptions options = null)
				: base(conventions, topology, channelFactory, options) { }

		protected override async Task<Acknowledgement> AcknowledgeMessageAsync(IPipeContext context)
		{
			var policy = context.GetPolicy(PolicyKeys.MessageAcknowledge);
			var result = await policy.ExecuteAsync(
				async ct => await base.AcknowledgeMessageAsync(context),
				CancellationToken.None);
			return result;
		}
	}
}
