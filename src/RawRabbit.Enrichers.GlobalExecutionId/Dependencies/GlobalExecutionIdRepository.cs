using System.Threading;

#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace RawRabbit.Enrichers.GlobalExecutionId.Dependencies
{
	public class GlobalExecutionIdRepository
	{
#if NET451
		protected const string GlobalExecutionId = "RawRabbit:GlobalExecutionId";
#else
		private static readonly AsyncLocal<string> GlobalExecutionId = new AsyncLocal<string>();
#endif
		
		public static string Get()
		{
#if NET451
			return CallContext.LogicalGetData(GlobalExecutionId) as string;
#else
			return GlobalExecutionId?.Value;
#endif
		}

		public static void Set(string id)
		{
#if NET451
			CallContext.LogicalSetData(GlobalExecutionId, id);
#else
			GlobalExecutionId.Value = id;
#endif
		}
	}
}
