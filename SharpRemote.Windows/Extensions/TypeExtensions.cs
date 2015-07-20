﻿using System;
using System.Linq;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	internal static class TypeExtensions
	{
#if !WINDOWS_PHONE_APP
		public static bool Is(this Type that, Type type)
		{
			if (that == null) throw new NullReferenceException();

			if (that.GetInterfaces().Any(x => x == type))
				return true;

			while (that != typeof(object) && that != null)
			{
				if (that == type)
					return true;

				that = that.BaseType;
			}

			return type == typeof(object);
		}
#endif
	}
}