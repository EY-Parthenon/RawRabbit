using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

		protected override async Task DeclareQueueAsync(QueueDeclaration queue, IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.QueueDeclare);
			await policy.ExecuteAsync(
				async ct => await base.DeclareQueueAsync(queue, context, ct),
				token);
		}
	}
}
