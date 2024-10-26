using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;

namespace Hosts;

static class ProgramMeta
{
	public static T GetAssemblyAttribute<T>()
	{
		object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false);
		return (attributes.Length == 0) ? default(T) : (T)attributes[0];
	}

	public static string GetTitle()
	{
		var attr = GetAssemblyAttribute<AssemblyTitleAttribute>();
		if (attr == null) return String.Empty;
		var title = attr.Title;

		var version = GetVersion();
		if (version != null)
		{
			title += " v" + version;
		}

#if DEBUG
		title += " DEBUG";
#endif

		var date = GetBuildDate();
		if (date != DateTime.MinValue)
		{
			// For some reason, it replaces "yyyy/MM/dd" by a locale specific date format
			// So, we use "-" as a delimiter and replace it to "/"
			title += date.ToString(" [yyyy-MM-dd]").Replace('-', '/');
		}

		return title;
	}

	public static string GetVersion()
	{
		var attr = GetAssemblyAttribute<AssemblyFileVersionAttribute>();
		if (attr == null) return null;

		var parts = new List<string>(attr.Version.TrimEnd('0', '.').Split('.'));
		if (parts.Count == 0) return null;
		while (parts.Count < 3)
		{
			parts.Add("0");
		}

		return String.Join(".", parts.ToArray());
	}

	public static DateTime GetBuildDate()
	{
		try
		{
			// Read it from the PE header
			var buffer = File.ReadAllBytes(Assembly.GetExecutingAssembly().Location);
			var pe = BitConverter.ToInt32(buffer, 0x3C);
			var time = BitConverter.ToInt32(buffer, pe + 8);
			return (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(time);
		}
		catch
		{
			return DateTime.MinValue;
		}
	}

	public static string GetCopyright()
	{
		var attr = GetAssemblyAttribute<AssemblyCopyrightAttribute>();
		return (attr == null) ? String.Empty : ("(C) " + attr.Copyright);
	}

	public static string GetDescription()
	{
		var attr = GetAssemblyAttribute<AssemblyDescriptionAttribute>();
		return (attr == null) ? String.Empty : attr.Description;
	}
}
