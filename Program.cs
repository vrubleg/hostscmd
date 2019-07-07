using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Reflection;
using System.Diagnostics;
using System.Security.Permissions;
using System.Security;

namespace Hosts
{
	class NoWritePermissionException : ApplicationException { }
	class HostNotSpecifiedException : ApplicationException { }
	class HostNotFoundException : ApplicationException 
	{
		public string Host { get; protected set; }
		public HostNotFoundException(string host) { Host = host; }
	}

	class Program
	{
		static bool IsUnix { get { return (Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX); } }
		static bool CanWrite;
		static string HostsFile;

		static string GetHostsFileName()
		{
			if (IsUnix)
			{
				return "/etc/hosts";
			}
			try
			{
				RegistryKey HostsRegKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\");
				string HostsPath = (string)HostsRegKey.GetValue("DataBasePath");
				HostsRegKey.Close();
				if (HostsPath.Trim() == "") throw new Exception("Empty path");
				return Environment.ExpandEnvironmentVariables(HostsPath + @"\hosts").ToLower();
			}
			catch (Exception e)
			{
				throw new Exception("Cannot get path to the hosts file from the registry", e);
			}
		}

		static T GetAssemblyAttribute<T>()
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false);
			return (attributes.Length == 0) ? default(T) : (T)attributes[0];
		}

		static string GetTitle()
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

		static string GetVersion()
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

		static DateTime GetBuildDate()
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

		static string GetCopyright()
		{
			var attr = GetAssemblyAttribute<AssemblyCopyrightAttribute>();
			return (attr == null) ? String.Empty : attr.Copyright;
		}

		static string GetDescription()
		{
			var attr = GetAssemblyAttribute<AssemblyDescriptionAttribute>();
			return (attr == null) ? String.Empty : attr.Description;
		}

		static void Help(bool interactive)
		{
			if (!interactive)
			{
				Console.WriteLine(GetTitle());
				Console.WriteLine(GetCopyright());
				Console.WriteLine();
				Console.WriteLine("Usage:");
				Console.WriteLine("  hosts - run hosts command interpreter");
				Console.WriteLine("  hosts <command> <params> - execute hosts command");
			}
			Console.WriteLine();
			Console.WriteLine("Commands:");
			Console.WriteLine("  add  <host> <aliases> <addr> # <comment>   - add new host");
			Console.WriteLine("  set  <host|mask> <addr> # <comment>        - set ip and comment for host");
			Console.WriteLine("  rem  <host|mask>   - remove host");
			Console.WriteLine("  on   <host|mask>   - enable host");
			Console.WriteLine("  off  <host|mask>   - disable host");
			Console.WriteLine("  view [all] <mask>  - display enabled and visible, or all hosts");
			Console.WriteLine("  hide <host|mask>   - hide host from 'hosts view'");
			Console.WriteLine("  show <host|mask>   - show host in 'hosts view'");
			Console.WriteLine("  print      - display raw hosts file");
			Console.WriteLine("  format     - format host rows");
			Console.WriteLine("  clean      - format and remove all comments");
			Console.WriteLine("  rollback   - rollback last operation");
			Console.WriteLine("  backup     - backup hosts file");
			Console.WriteLine("  restore    - restore hosts file from backup");
			Console.WriteLine("  empty      - empty hosts file");
			if (!IsUnix)
			{
				Console.WriteLine("  open       - open hosts file in notepad");
			}
			if (interactive)
			{
				Console.WriteLine("  exit       - exit from command interpreter");
			}
			Console.WriteLine();
			Console.WriteLine("Details:\n  " + GetDescription());
		}

		static HostsEditor Hosts;
		static Queue<string> ArgsQueue;
	
		static void View(string mask, bool? visibleOnly = null, bool? enabledOnly = null)
		{
			int enabled = 0;
			int disabled = 0;
			int hidden = 0;

			if (mask != "*") Console.WriteLine("Mask: {0}\n", mask);

			Hosts.RemoveInvalid();
			Hosts.ResetFormat();
			List<HostsItem> FoundLines = Hosts.GetMatched(mask);
			foreach (HostsItem Line in FoundLines)
			{
				if (Line.Enabled) enabled++; else disabled++;
				if (Line.Hidden) hidden++;
				if (visibleOnly != null && visibleOnly.Value == Line.Hidden) continue;
				if (enabledOnly != null && enabledOnly.Value != Line.Enabled) continue;
				Console.WriteLine(Line);
			}
			if (FoundLines.Count > 0) Console.WriteLine();
			Console.WriteLine("Enabled: {0,-4} Disabled: {1,-4} Hidden: {2,-4}", enabled, disabled, hidden);
		}

		static void Run(string[] args, bool interactive)
		{
			try
			{
				ArgsQueue = new Queue<string>(args);
				string Mode = (ArgsQueue.Count > 0) ? ArgsQueue.Dequeue().ToLower() : "help";
				string BackupFile = HostsFile + ".backup";
				string RollbackFile = HostsFile + ".rollback";

				switch (Mode)
				{
					case "open":
						if (IsUnix) break;
						var exe = FileAssoc.GetExecutable(".txt") ?? "notepad";
						Process.Start(exe, '"' + HostsFile + '"');
						return;

					case "apply":
						if (!CanWrite) throw new NoWritePermissionException();
						var ApplyHostsFile = ArgsQueue.Dequeue();
						if (!File.Exists(ApplyHostsFile)) throw new Exception("Applied file does not exist");
						File.Copy(HostsFile, RollbackFile, true);
						File.Copy(ApplyHostsFile, HostsFile, true);
						Console.WriteLine("[OK] New hosts file applied successfully");
						return;

					case "backup":
						if (!CanWrite) throw new NoWritePermissionException();
						if (ArgsQueue.Count > 0) BackupFile = HostsFile + "." + ArgsQueue.Dequeue().ToLower();
						File.Copy(HostsFile, BackupFile, true);
						Console.WriteLine("[OK] Hosts file backed up successfully");
						return;

					case "restore":
						if (!CanWrite) throw new NoWritePermissionException();
						if (ArgsQueue.Count > 0) BackupFile = HostsFile + "." + ArgsQueue.Dequeue().ToLower();
						if (!File.Exists(BackupFile)) throw new Exception("Backup file does not exist");
						File.Copy(HostsFile, RollbackFile, true);
						File.Copy(BackupFile, HostsFile, true);
						Console.WriteLine("[OK] Hosts file restored successfully");
						return;

					case "rollback":
						if (!CanWrite) throw new NoWritePermissionException();
						if (!File.Exists(RollbackFile)) throw new Exception("Rollback file does not exist");
						if (File.Exists(HostsFile)) File.Delete(HostsFile);
						File.Move(RollbackFile, HostsFile);
						Console.WriteLine("[OK] Hosts file rolled back successfully");
						return;

					case "empty":
					case "recreate":
						if (!CanWrite) throw new NoWritePermissionException();
						File.Copy(HostsFile, RollbackFile, true);
						File.WriteAllText(HostsFile, new HostsItem("127.0.0.1", "localhost").ToString());
						Console.WriteLine("[OK] New hosts file created successfully");
						return;

					case "help":
						Help(interactive);
						return;
				}

				Hosts = new HostsEditor(HostsFile);
				Hosts.Load();

				List<HostsItem> Lines;
				switch (Mode)
				{
					case "print":
					case "raw":
					case "file":
						Console.WriteLine(File.ReadAllText(Hosts.FileName, Hosts.Encoding));
						return;

					case "list":
					case "view":
					case "select":
					case "ls":
						RunListMode(interactive);
						return;

					case "format":
						if (!CanWrite) throw new NoWritePermissionException();
						Hosts.ResetFormat();
						Console.WriteLine("[OK] Hosts file formatted successfully");
						break;

					case "clean":
						if (!CanWrite) throw new NoWritePermissionException();
						Hosts.RemoveInvalid();
						Hosts.ResetFormat();
						Console.WriteLine("[OK] Hosts file cleaned successfully");
						break;

					case "add":
					case "new":
						if (!CanWrite) throw new NoWritePermissionException();
						RunAddMode();
						break;

					case "set":
						if (!CanWrite) throw new NoWritePermissionException();
						RunUpdateMode(true);
						break;

					case "change":
					case "update":
					case "upd":
						if (!CanWrite) throw new NoWritePermissionException();
						RunUpdateMode(false);
						break;

					case "rem":
					case "rm":
					case "remove":
					case "del":
					case "delete":
						if (!CanWrite) throw new NoWritePermissionException();
						if (ArgsQueue.Count == 0) throw new HostNotSpecifiedException();
						Lines = Hosts.GetMatched(args[1]);
						if (Lines.Count == 0) throw new HostNotFoundException(args[1]);
						foreach (HostsItem Line in Lines)
						{
							Hosts.Remove(Line);
							Console.WriteLine("[REMOVED] {0} {1}", Line.IP.ToString(), Line.Aliases.ToString());
						}
						break;

					case "on":
					case "enable":
						if (!CanWrite) throw new NoWritePermissionException();
						if (ArgsQueue.Count == 0) throw new HostNotSpecifiedException();
						Lines = Hosts.GetMatched(args[1]);
						if (Lines.Count == 0) throw new HostNotFoundException(args[1]);
						foreach (HostsItem Line in Lines)
						{
							Line.Enabled = true;
							Console.WriteLine("[ENABLED] {0} {1}", Line.IP.ToString(), Line.Aliases.ToString());
						}
						break;

					case "off":
					case "disable":
						if (!CanWrite) throw new NoWritePermissionException();
						if (ArgsQueue.Count == 0) throw new HostNotSpecifiedException();
						Lines = Hosts.GetMatched(args[1]);
						if (Lines.Count == 0) throw new HostNotFoundException(args[1]);
						foreach (HostsItem Line in Lines)
						{
							Line.Enabled = false;
							Console.WriteLine("[DISABLED] {0} {1}", Line.IP.ToString(), Line.Aliases.ToString());
						}
						break;

					case "hide":
						if (!CanWrite) throw new NoWritePermissionException();
						if (ArgsQueue.Count == 0) throw new HostNotSpecifiedException();
						Lines = Hosts.GetMatched(args[1]);
						if (Lines.Count == 0) throw new HostNotFoundException(args[1]);
						foreach (HostsItem Line in Lines)
						{
							Line.Hidden = true;
							Console.WriteLine("[HIDDEN] {0} {1}", Line.IP.ToString(), Line.Aliases.ToString());
						}
						break;

					case "show":
						if (!CanWrite) throw new NoWritePermissionException();
						if (ArgsQueue.Count == 0) throw new HostNotSpecifiedException();
						Lines = Hosts.GetMatched(args[1]);
						if (Lines.Count == 0) throw new HostNotFoundException(args[1]);
						foreach (HostsItem Line in Lines)
						{
							Line.Hidden = false;
							Console.WriteLine("[SHOWN] {0} {1}", Line.IP.ToString(), Line.Aliases.ToString());
						}
						break;

					default:
						Console.WriteLine("[ERROR] Unknown command");
						Help(interactive);
						return;
				}
				File.Copy(HostsFile, RollbackFile, true);
				Hosts.Save();
			}
			catch (NoWritePermissionException)
			{
				Console.WriteLine("[ERROR] No write permission to the hosts file");
			}
			catch (HostNotSpecifiedException)
			{
				Console.WriteLine("[ERROR] Host not specified");
			}
			catch (HostNotFoundException e)
			{
				Console.WriteLine("[ERROR] Host '{0}' not found", e.Host);
			}
			catch (Exception e)
			{
#if DEBUG
				Console.WriteLine("[ERROR] " + e.ToString());
#else
				Console.WriteLine("[ERROR] " + e.Message);
#endif
			}
		}

		static void RunListMode(bool interactive)
		{
			if (!interactive)
			{
				Console.WriteLine(GetTitle());
				Console.WriteLine("Hosts file: " + Hosts.FileName.ToLower());
				Console.WriteLine();
			}

			bool? visibleOnly = true;
			bool? enabledOnly = true;
			string mask = "*";

			if (ArgsQueue.Count > 0)
			{
				string arg = ArgsQueue.Dequeue().ToLower();
				if (arg == "all")
				{
					visibleOnly = null;
					enabledOnly = null;
					arg = (ArgsQueue.Count > 0) ? ArgsQueue.Dequeue().ToLower() : "*";
				}
				mask = arg;
				if (!mask.StartsWith("*")) mask = '*' + mask;
				if (!mask.EndsWith("*")) mask += '*';
			}

			View(mask, visibleOnly, enabledOnly);
		}

		static void AddHostsItem(NetAddress address, HostAliases aliases, string comment)
		{
			if (aliases.Count == 0) throw new HostNotSpecifiedException();

			// Remove duplicates
			foreach (HostName host in aliases)
			{
				var lines = Hosts.FindAll(item => item.Valid && item.IP.Type == address.Type && item.Aliases.Contains(host));
				foreach (var line in lines)
				{
					if (line.Aliases.Count == 1)
					{
						Hosts.Remove(line);
						Console.WriteLine("[REMOVED] {0} {1}", line.IP.ToString(), line.Aliases.ToString());
					}
					else
					{
						line.Aliases.Remove(host);
						Console.WriteLine("[UPDATED] {0} {1}", line.IP.ToString(), line.Aliases.ToString());
					}
				}
			}

			// New host
			var new_item = new HostsItem(address, aliases, comment == null ? "" : comment.Trim());
			Hosts.Add(new_item);
			Console.WriteLine("[ADDED] {0} {1}", new_item.IP.ToString(), new_item.Aliases.ToString());
		}

		static void RunAddMode()
		{
			if (ArgsQueue.Count == 0) throw new HostNotSpecifiedException();
			HostAliases aliases = new HostAliases();
			NetAddress address_ipv4 = null;
			NetAddress address_ipv6 = null;
			string comment = "";
			bool in_comment = false;

			while (ArgsQueue.Count > 0)
			{
				string arg = ArgsQueue.Dequeue();
				if (in_comment)
				{
					comment += arg + " ";
					continue;
				}
				if (arg.Length > 0 && arg[0] == '#')
				{
					in_comment = true;
					comment = (arg.Length > 1) ? (arg.Substring(1) + " ") : "";
					continue;
				}
				arg = arg.ToLower();
				var address_test = NetAddress.TryCreate(arg);
				if (address_test != null)
				{
					if (address_test.Type == NetAddressType.IPv4)
						address_ipv4 = address_test;
					else
						address_ipv6 = address_test;
					continue;
				}
				var hostname_test = HostName.TryCreate(arg);
				if (hostname_test != null)
				{
					aliases.Add(hostname_test);
					continue;
				}
				throw new Exception(String.Format("Unknown argument '{0}'", arg));
			}

			if (address_ipv4 == null && address_ipv6 == null)
			{
				address_ipv4 = new NetAddress("127.0.0.1");
			}

			if (address_ipv4 != null)
			{
				AddHostsItem(address_ipv4, aliases, comment.Trim());
			}

			if (address_ipv6 != null)
			{
				AddHostsItem(address_ipv6, aliases, comment.Trim());
			}
		}

		static void RunUpdateMode(bool autoadd = false)
		{
			if (ArgsQueue.Count == 0) throw new HostNotSpecifiedException();
			string mask = ArgsQueue.Dequeue();
			List<HostsItem> lines = Hosts.GetMatched(mask);
			if (lines.Count == 0 && (!autoadd || mask.IndexOf('*') != -1))
			{
				throw new HostNotFoundException(mask);
			}

			NetAddress address_ipv4 = null;
			NetAddress address_ipv6 = null;
			string comment = null;
			bool in_comment = false;

			while (ArgsQueue.Count > 0)
			{
				string arg = ArgsQueue.Dequeue();
				if (in_comment)
				{
					comment += arg + " ";
					continue;
				}
				if (arg.Length > 0 && arg[0] == '#')
				{
					in_comment = true;
					comment = (arg.Length > 1) ? (arg.Substring(1) + " ") : "";
					continue;
				}
				arg = arg.ToLower();
				NetAddress address_test = NetAddress.TryCreate(arg);
				if (address_test != null)
				{
					if (address_test.Type == NetAddressType.IPv4)
						address_ipv4 = address_test;
					else
						address_ipv6 = address_test;
					continue;
				}
			}

			var ipv4_added = false;
			var ipv6_added = false;

			foreach (HostsItem line in lines)
			{
				if (address_ipv4 == null && address_ipv6 == null && comment != null)
				{
					// Update comments only
					line.Comment = comment;
					Console.WriteLine("[UPDATED] {0} {1}", line.IP.ToString(), line.Aliases.ToString());
					continue;
				}
				if (address_ipv4 != null && line.IP.Type == NetAddressType.IPv4)
				{
					ipv4_added = true;
					line.IP = address_ipv4;
					if (comment != null) line.Comment = comment;
					Console.WriteLine("[UPDATED] {0} {1}", line.IP.ToString(), line.Aliases.ToString());
				}
				if (address_ipv6 != null && line.IP.Type == NetAddressType.IPv6)
				{
					ipv6_added = true;
					line.IP = address_ipv6;
					if (comment != null) line.Comment = comment;
					Console.WriteLine("[UPDATED] {0} {1}", line.IP.ToString(), line.Aliases.ToString());
				}
			}

			if (address_ipv4 != null && !ipv4_added && autoadd)
			{
				AddHostsItem(address_ipv4, new HostAliases(mask), comment);
			}

			if (address_ipv6 != null && !ipv6_added && autoadd)
			{
				AddHostsItem(address_ipv6, new HostAliases(mask), comment);
			}
		}

		static void Init()
		{
			HostsFile = GetHostsFileName();

			try
			{
				// Create default hosts file if not exists
				if (!File.Exists(HostsFile))
				{
					File.WriteAllText(HostsFile, new HostsItem("127.0.0.1", "localhost").ToString());
				}

				// Try to create backup on first run
				var BackupFile = HostsFile + ".backup";
				if (!File.Exists(BackupFile))
				{
					File.Copy(HostsFile, BackupFile);
				}

				// Check if hosts file is writeable
				var CheckFile = HostsFile + ".check";
				File.Copy(HostsFile, CheckFile, true);
				File.AppendAllText(HostsFile, "\r\n127.0.0.1 checkhost.local");
				File.Copy(CheckFile, HostsFile, true);
				File.Delete(CheckFile);

				CanWrite = true;
			}
			catch
			{
				CanWrite = false;
			}
		}

		static void Main(string[] args)
		{
			try
			{
				Init();

				if (args.Length > 0)
				{
					Run(args, false);
				}
				else
				{
					Console.WriteLine(GetTitle());
					Console.WriteLine(GetCopyright());
					Console.WriteLine("Hosts file: " + HostsFile.ToLower());
					Console.WriteLine();
					while (true)
					{
						Console.Write("hosts> ");
						var command = (Console.ReadLine() ?? "").Replace("\0", "").Trim();
						if (command == "") continue;
						if (command.StartsWith("hosts "))
						{
							command = command.Substring(6).TrimStart();
						}
						if (command == "exit" || command == "quit") break;
						args = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						Run(args, true);
						Console.WriteLine();
					}
				}
			}
			catch (Exception e)
			{
				#if DEBUG
				Console.WriteLine("[ERROR] " + e.ToString());
				#else
				Console.WriteLine("[ERROR] " + e.Message);
				#endif
				Console.ReadKey();
			}
		}
	}
}
