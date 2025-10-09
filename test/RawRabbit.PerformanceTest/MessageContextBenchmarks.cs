using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Instantiation;

namespace RawRabbit.PerformanceTest
{
	public class MessageContextBenchmarks
	{
		private IBusClient _withoutContext = null!;
		private Task _completedTask = null!;
		private MessageA _messageA = null!;
		private IBusClient _withContext = null!;
		private MessageB _messageB = null!;
		public event EventHandler? MessageReceived;

		[GlobalSetup]
		public void Setup()
		{
			_withoutContext = RawRabbitFactory.CreateSingleton();
			_withContext = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
			{
				Plugins = p => p.UseMessageContext<MessageContext>()
			});
			_completedTask = Task.FromResult(0);
			_messageA = new MessageA();
			_messageB = new MessageB();
			_withoutContext.SubscribeAsync<MessageA>(message =>
			{
				MessageReceived?.Invoke(message, EventArgs.Empty);
				return _completedTask;
			});
			_withContext.SubscribeAsync<MessageB, MessageContext>((message, context) =>
			{
				MessageReceived?.Invoke(message, EventArgs.Empty);
				return _completedTask;
			});
		}

		[GlobalCleanup]
		public void Cleanup()
		{
			_withoutContext.DeleteQueueAsync<MessageA>();
			_withoutContext.DeleteQueueAsync<MessageB>();
			(_withoutContext as IDisposable)?.Dispose();
			(_withContext as IDisposable)?.Dispose();
		}

		[Benchmark]
		public async Task MessageContext_FromFactory()
		{
			var msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult((sender as Message)!); };
			MessageReceived += onMessageReceived;

			_ = _withContext.PublishAsync(_messageB);
			await msgTsc.Task;
			MessageReceived -= onMessageReceived;
		}

		[Benchmark]
		public async Task MessageContext_None()
		{
			var msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult((sender as Message)!); };
			MessageReceived += onMessageReceived;

			_ = _withoutContext.PublishAsync(_messageA);
			await msgTsc.Task;
			MessageReceived -= onMessageReceived;
		}


		public class MessageA { }
		public class MessageB { }
		public class MessageContext { }
	}
}
