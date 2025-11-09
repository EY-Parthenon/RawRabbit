using Ninject;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Ninject
{
	public static class KernelExtension
	{
		// Ninject 3.3.6: Use IKernel for all .NET versions
		public static IKernel RegisterRawRabbit(this IKernel config, RawRabbitOptions options = null)
		{
			if (options != null)
			{
				config.Bind<RawRabbitOptions>().ToConstant(options);
			}
			config.Load<RawRabbitModule>();
			return config;
		}
	}
}
