using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SharpRemote
{
	/// <summary>
	///     Similar to <see cref="MethodInfo" /> (in that it describes a particular .NET method), but only
	///     describes its static structure that is important to a <see cref="ISerializer" />.
	/// </summary>
	[DataContract]
	public sealed class MethodDescription
		: IMethodDescription
	{
		/// <inheritdoc />
		[DataMember]
		public ParameterDescription ReturnParameter { get; set; }

		/// <inheritdoc />
		public TypeDescription ReturnType => ReturnParameter?.ParameterType;

		/// <summary>
		///     The equivalent of <see cref="MethodBase.GetParameters" />.
		/// </summary>
		[DataMember]
		public ParameterDescription[] Parameters { get; set; }

		IReadOnlyList<ParameterDescription> IMethodDescription.Parameters => Parameters;
	}
}