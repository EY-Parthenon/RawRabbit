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
		private readonly AsyncLocal<object> _msgContext;
#endif

		public MessageContextRepository()
		{
#if !NET451
			_msgContext = new AsyncLocal<object>();
#endif
		}
		public object Get()
		{
#if NET451
			return CallContext.LogicalGetData(MessageContext) as object;
#else
			return _msgContext?.Value;
#endif
		}

		public void Set(object context)
		{
#if NET451
			CallContext.LogicalSetData(MessageContext, context);
#else
			_msgContext.Value = context;
#endif
		}
	}
}
