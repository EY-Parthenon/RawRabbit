using System;
using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Instantiation;
using Xunit;
using Xunit.Abstractions;

namespace RawRabbit.IntegrationTests.PublisherConfirms
{
	public class MinimalPublisherConfirmsTest : IDisposable
	{
		private readonly ITestOutputHelper _output;

		public MinimalPublisherConfirmsTest(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public async Task Should_Receive_Publisher_Confirms_On_Simple_Publish()
		{
			_output.WriteLine("=== TEST START ===");
			_output.WriteLine($"Time: {DateTime.Now:O}");

			// Create a test client with publisher confirms enabled
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				_output.WriteLine("Client created");

				var message = new BasicMessage { Prop = "Test message for publisher confirms" };
				_output.WriteLine($"Publishing message: {message.Prop}");

				try
				{
					// This should complete successfully if publisher confirms are working
					await client.PublishAsync(message);
					_output.WriteLine("SUCCESS: Message published and confirmed!");
				}
				catch (Exception ex)
				{
					_output.WriteLine($"FAILED: {ex.GetType().Name}: {ex.Message}");
					_output.WriteLine($"Stack trace: {ex.StackTrace}");
					throw;
				}
			}

			_output.WriteLine("=== TEST END ===");
		}

		[Fact]
		public async Task Should_Receive_Publisher_Confirms_On_Multiple_Publishes()
		{
			_output.WriteLine("=== MULTIPLE PUBLISH TEST START ===");
			_output.WriteLine($"Time: {DateTime.Now:O}");

			using (var client = RawRabbitFactory.CreateTestClient())
			{
				_output.WriteLine("Client created");

				for (int i = 0; i < 5; i++)
				{
					var message = new BasicMessage { Prop = $"Message {i + 1}" };
					_output.WriteLine($"Publishing message {i + 1}: {message.Prop}");

					try
					{
						await client.PublishAsync(message);
						_output.WriteLine($"SUCCESS: Message {i + 1} confirmed");
					}
					catch (Exception ex)
					{
						_output.WriteLine($"FAILED on message {i + 1}: {ex.GetType().Name}: {ex.Message}");
						throw;
					}
				}
			}

			_output.WriteLine("=== MULTIPLE PUBLISH TEST END ===");
		}

		public void Dispose()
		{
			// Cleanup
		}
	}
}
