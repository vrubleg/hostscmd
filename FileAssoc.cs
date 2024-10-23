using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Hosts;

internal static class FileAssoc
{
	[DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
	static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder sOut, [In][Out] ref uint nOut);

	[Flags]
	public enum AssocF
	{
		Init_NoRemapCLSID = 0x1,
		Init_ByExeName = 0x2,
		Open_ByExeName = 0x2,
		Init_DefaultToStar = 0x4,
		Init_DefaultToFolder = 0x8,
		NoUserSettings = 0x10,
		NoTruncate = 0x20,
		Verify = 0x40,
		RemapRunDll = 0x80,
		NoFixUps = 0x100,
		IgnoreBaseClass = 0x200
	}

	enum AssocStr
	{
		Command = 1,
		Executable,
		FriendlyDocName,
		FriendlyAppName,
		NoOpen,
		ShellNewValue,
		DDECommand,
		DDEIfExec,
		DDEApplication,
		DDETopic
	}

	public static string GetExecutable(string doctype)
	{
		uint out_path_size = 0;
		if (AssocQueryString(AssocF.Verify, AssocStr.Executable, doctype, null, null, ref out_path_size) != 1)
		{
			return null;
		}

		StringBuilder out_path = new StringBuilder((int)out_path_size);
		if (AssocQueryString(AssocF.Verify, AssocStr.Executable, doctype, null, out_path, ref out_path_size) != 0)
		{
			return null;
		}

		return out_path.ToString();
	}
}
