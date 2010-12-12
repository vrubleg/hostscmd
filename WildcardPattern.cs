using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hosts
{
	public class WildcardPattern
	{
		private Regex regex;

		public WildcardPattern(string pattern)
		{
			pattern = "^" + pattern.Replace(".", @"\.").Replace("?", ".").Replace("*", ".*") + "$";
			regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		}

		public bool IsMatch(string text)
		{
			return regex.IsMatch(text);
		}
	}
}
