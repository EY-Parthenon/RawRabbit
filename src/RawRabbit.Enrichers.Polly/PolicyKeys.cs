namespace RawRabbit.Enrichers.Polly
{
	/// <summary>
	/// Constants for policy keys used to store ResiliencePipelines in IPipeContext.
	/// Polly 8.x: Use these keys with ResiliencePipeline instances instead of Policy.
	/// Example:
	/// <code>
	/// var pipeline = new ResiliencePipelineBuilder()
	///     .AddRetry(new RetryStrategyOptions
	///     {
	///         MaxRetryAttempts = 3,
	///         ShouldHandle = new PredicateBuilder().Handle&lt;BrokerUnreachableException&gt;()
	///     })
	///     .Build();
	/// context.Properties.Add(PolicyKeys.BasicPublish, pipeline);
	/// </code>
	/// </summary>
	public class PolicyKeys
	{
		/// <summary>
		/// Policy key for message acknowledgement operations.
		/// </summary>
		public const string MessageAcknowledge = "MessageAcknowledge";

		/// <summary>
		/// Policy key for default fallback policy used when no specific policy is found.
		/// </summary>
		public const string DefaultPolicy = "DefaultPolicy";

		/// <summary>
		/// Policy key for consumer creation operations.
		/// </summary>
		public const string ConsumerCreate = "ConsumerCreate";

		/// <summary>
		/// Policy key for channel creation operations.
		/// </summary>
		public const string ChannelCreate = "ChannelCreate";

		/// <summary>
		/// Policy key for queue declaration operations.
		/// </summary>
		public const string QueueDeclare = "QueueDeclare";

		/// <summary>
		/// Policy key for queue binding operations.
		/// </summary>
		public const string QueueBind = "QueueBind";

		/// <summary>
		/// Policy key for exchange declaration operations.
		/// </summary>
		public const string ExchangeDeclare = "ExchangeDeclare";

		/// <summary>
		/// Policy key for basic publish operations.
		/// </summary>
		public const string BasicPublish = "BasicPublish";

		/// <summary>
		/// Policy key for message handler invocation operations.
		/// </summary>
		public const string HandlerInvocation = "HandlerInvocation";
	}
}
