using System.IO;

namespace SharpRemote
{
	public interface IEndPointChannel
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="servantId"></param>
		/// <param name="interfaceType"></param>
		/// <param name="methodName"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		MemoryStream CallRemoteMethod(ulong servantId, string interfaceType, string methodName, MemoryStream arguments);
	}
}