using System;
using Microsoft.Win32;
using System.Linq;
using System.Text.RegularExpressions;

namespace EventScriptIDE
{
	public static class OSChecker
	{
		static readonly string[] SupportedOSes =
		{
			"Windows XP",
			"Windows Vista",
			"Windows 7",
			"Windows Embedded 8.1",
			"Windows 8",
			"Windows 8.1",
			"Windows 10"
		};

		public static bool CheckOS()
		{
			bool supported = false;
			string productName = Registry.GetValue(
				@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
				"ProductName",
				null
			)?.ToString();

			if (string.IsNullOrEmpty(productName))
				return false;
			
			string osName = Regex.Replace(productName, @"^Microsoft\s+", "");
			
			osName = Regex.Replace(
				osName,
				@"\s+(Home|Pro|Professional|Enterprise|Industry|Education|IoT|LTSC|LTSB|N|KN|K|Core|Single Language).*",
				"",
				RegexOptions.IgnoreCase
			);

			string buildStr = Registry.GetValue(
				@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
				"CurrentBuildNumber",
				null
			)?.ToString();

			int build = 0;
			int.TryParse(buildStr, out build);

			if (build>=2222 )
			{
				supported = SupportedOSes.Any(v => osName.Contains(v));
			}

			return supported;
		}
	}
}
