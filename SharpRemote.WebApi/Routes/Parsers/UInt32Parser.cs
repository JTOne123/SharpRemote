using System;
using System.Globalization;

namespace SharpRemote.WebApi.Routes.Parsers
{
	internal sealed class UInt32Parser
		: ArgumentParser
	{
		public override bool TryExtract(string str,
			int start,
			out object value,
			out int consumed)
		{
			var tmp = str.Substring(start);

			uint number;
			if (UInt32.TryParse(tmp, NumberStyles.Integer, CultureInfo.CurrentCulture, out number))
			{
				var digits = number == 0
					? 1
					: (int)Math.Floor(Math.Log10(number) + 1);
				consumed = digits;
				value = number;
				return true;
			}

			consumed = 0;
			value = null;
			return false;
		}
	}
}