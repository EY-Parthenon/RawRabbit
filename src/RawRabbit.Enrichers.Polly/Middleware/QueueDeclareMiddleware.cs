using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class QueueDeclareMiddleware : Pipe.Middleware.QueueDeclareMiddleware
	{
		public QueueDeclareMiddleware(ITopologyProvider topology, QueueDeclareOptions options = null)
				: base(topology, options)
		{
		}

		protected override Task DeclareQueueAsync(QueueDeclaration queue, IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.QueueDeclare);
			var pollyContext = new Context
			{
				[RetryKey.TopologyProvider] = Topology,
				[RetryKey.QueueDeclaration] = queue,
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token,
			};
			return policy.ExecuteAsync(
				action: (ctx, ct) => base.DeclareQueueAsync(queue, context, ct),
				context: pollyContext,
				cancellationToken: token);
		}
	}
}
