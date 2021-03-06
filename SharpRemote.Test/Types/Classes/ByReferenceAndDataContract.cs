﻿using System.Runtime.Serialization;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class ByReferenceAndDataContract
		: IByReferenceType
	{
		public int Value
		{
			get { return GetHashCode(); }
		}
	}
}