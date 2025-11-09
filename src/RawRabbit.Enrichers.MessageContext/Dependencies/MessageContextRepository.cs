using System.Threading;

#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace RawRabbit.Enrichers.MessageContext.Dependencies
{
	public interface IMessageContextRepository
	{
		object Get();
		void Set(object context);
	}

	public class MessageContextRepository : IMessageContextRepository
	{

#if NET451
		private const string MessageContext = "RawRabbit:MessageContext";
#else
		// .NET Standard 1.5+ and .NET 8+
		private readonly AsyncLocal<object> _msgContext;
#endif

		public MessageContextRepository()
		{
#if !NET451
			// .NET Standard 1.5+ and .NET 8+
			_msgContext = new AsyncLocal<object>();
#endif
		}
		public object Get()
		{
#if NET451
			return CallContext.LogicalGetData(MessageContext) as object;
#else
			// .NET Standard 1.5+ and .NET 8+
			return _msgContext?.Value;
#endif
		}

		public void Set(object context)
		{
#if NET451
			CallContext.LogicalSetData(MessageContext, context);
#else
			// .NET Standard 1.5+ and .NET 8+
			_msgContext.Value = context;
#endif
		}
	}
}
