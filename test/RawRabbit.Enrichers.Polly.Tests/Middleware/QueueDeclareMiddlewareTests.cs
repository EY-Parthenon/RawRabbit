using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using QueueDeclareMiddleware = RawRabbit.Enrichers.Polly.Middleware.QueueDeclareMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class QueueDeclareMiddlewareTests
	{
		[Fact]
		public async Task Should_Invoke_Queue_Declare_Policy_With_Correct_Context()
		{
			var topology = new Mock<ITopologyProvider>();
			var queueDeclaration = new QueueDeclaration();
			var policyCalled = false;

			topology
				.SetupSequence(t => t.DeclareQueueAsync(queueDeclaration))
				.Throws(new OperationInterruptedException(null))
				.Returns(Task.CompletedTask);

			var context = new PipeContext
			{
				Properties = new Dictionary<string, object>
				{
					{PipeKey.QueueDeclaration, queueDeclaration}
				}
			};

			var pipeline = new ResiliencePipelineBuilder()
				.AddRetry(new RetryStrategyOptions
				{
					ShouldHandle = new PredicateBuilder().Handle<OperationInterruptedException>(),
					MaxRetryAttempts = 2,
					Delay = TimeSpan.Zero,
					OnRetry = args =>
					{
						policyCalled = true;
						return default;
					}
				})
				.Build();

			context.UsePolicy(pipeline, PolicyKeys.QueueDeclare);
			var middleware = new QueueDeclareMiddleware(topology.Object) {Next = new NoOpMiddleware()};

			/* Test */
			await middleware.InvokeAsync(context);

			/* Assert */
			Assert.True(policyCalled, "Should call policy and retry on failure");
			topology.Verify(t => t.DeclareQueueAsync(queueDeclaration), Times.Exactly(2));
		}
	}
}
