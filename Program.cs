using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Reflection;
using System.Diagnostics;
using System.Security.Permissions;
using System.Security;

namespace Hosts;

class NoWritePermissionException : ApplicationException { }
class HostNotSpecifiedException : ApplicationException { }
class HostNotFoundException : ApplicationException 
{
	public string Host { get; protected set; }
	public HostNotFoundException(string host) { Host = host; }
}

static class Program
{
	static bool IsShell = false;
	static readonly bool IsUnix = (Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX);

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
			throw new Exception("Cannot get path to hosts file from registry.", e);
		}
	}

	static bool MakeWritable(string filename)
	{
		try
		{
			if (File.Exists(filename))
			{
				// Remove ReadOnly flag if it is there.
				FileAttributes attributes = File.GetAttributes(filename);
				if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				{
					File.SetAttributes(filename, attributes & ~FileAttributes.ReadOnly);
				}
			}
			File.OpenWrite(filename).Close();
			return true;
		}
		catch
		{
			return false;
		}
	}

	static void ForceCopy(string src, string dst)
	{
		MakeWritable(dst);
		File.Copy(src, dst, true);
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

	static void Help()
	{
		if (!IsShell) Console.WriteLine($"""
			{GetTitle()}
			{GetCopyright()}

			{GetDescription()}

			Usage:
			    hosts [shell]       Run interactive shell.
			    hosts <command>     Execute a command.

			""");
		Console.WriteLine("""
			Commands:
			    add  <host> [aliases] [ipv4] [ipv6] # [comment]      Add new host.
			    set  <host|mask> [ipv4] [ipv6] # [comment]           Update host.
			    del  <host|mask>        Delete host.
			    on   <host|mask>        Enable host.
			    off  <host|mask>        Disable host.
			    list [--all] [mask]     Display active or all hosts.
			    hide <host|mask>        Hide host from 'list'.
			    unhide <host|mask>      Unhide host.
			    print       Display raw hosts file.
			    format      Reformat hosts file.
			    clean       Reformat and delete all comments.
			    rollback    Rollback last operation.
			    backup      Backup hosts file.
			    restore     Restore hosts file from backup.
			    reset       Reset hosts file (remove everything).
			""");
		if (!IsUnix) Console.WriteLine("""
			    open        Open hosts file in notepad.
			""");
		if (IsShell) Console.WriteLine("""
			    exit        Exit from the shell.
			""");
	}

	static HostsEditor Hosts;

	static bool Execute(List<string> args)
	{
		try
		{
			var args_queue = new Queue<string>(args);
			var mode = (args_queue.Count > 0) ? args_queue.Dequeue().ToLower() : "help";
			var hosts_file = Hosts.FileName;
			var backup_file = hosts_file + ".backup";
			var rollback_file = hosts_file + ".rollback";

			switch (mode)
			{
				case "open":
					if (IsUnix) break;
					var exe = FileAssoc.GetExecutable(".txt") ?? "notepad";
					Process.Start(exe, '"' + hosts_file + '"');
					return true;

				case "apply":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					if (args_queue.Count == 0) throw new Exception("Applied file is not specified.");
					var apply_file = args_queue.Dequeue();
					if (!File.Exists(apply_file)) throw new Exception("Applied file does not exist.");
					ForceCopy(hosts_file, rollback_file);
					ForceCopy(apply_file, hosts_file);
					Console.WriteLine("[OK] New hosts file is applied successfully.");
					return true;

				case "backup":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					if (args_queue.Count > 0) backup_file = hosts_file + "." + args_queue.Dequeue().ToLower();
					ForceCopy(hosts_file, backup_file);
					Console.WriteLine("[OK] Hosts file is backed up successfully.");
					return true;

				case "restore":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					if (args_queue.Count > 0) backup_file = hosts_file + "." + args_queue.Dequeue().ToLower();
					if (!File.Exists(backup_file)) throw new Exception("Backup file does not exist.");
					ForceCopy(hosts_file, rollback_file);
					ForceCopy(backup_file, hosts_file);
					Console.WriteLine("[OK] Hosts file is restored successfully.");
					return true;

				case "rollback":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					if (!File.Exists(rollback_file)) throw new Exception("Rollback file does not exist.");
					if (File.Exists(hosts_file)) File.Delete(hosts_file);
					File.Move(rollback_file, hosts_file);
					Console.WriteLine("[OK] Hosts file is rolled back successfully.");
					return true;

				case "reset":
				case "empty":
				case "recreate":
				case "erase":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					ForceCopy(hosts_file, rollback_file);
					File.WriteAllText(hosts_file, new HostsItem("127.0.0.1", "localhost").ToString());
					Console.WriteLine("[OK] New hosts file is created successfully.");
					return true;

				case "help":
				case "--help":
				case "-h":
				case "/?":
					Help();
					return true;
			}

			Hosts.Load();

			List<HostsItem> Lines;
			switch (mode)
			{
				case "print":
				case "raw":
				case "file":
					Console.WriteLine(File.ReadAllText(Hosts.FileName, Hosts.Encoding));
					return true;

				case "list":
				case "view":
				case "select":
				case "ls":
				case "show":
					RunListMode(args_queue.ToList());
					return true;

				case "format":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					Hosts.ResetFormat();
					Console.WriteLine("[OK] Hosts file is formatted successfully.");
					break;

				case "clean":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					Hosts.RemoveInvalid();
					Hosts.ResetFormat();
					Console.WriteLine("[OK] Hosts file is cleaned successfully.");
					break;

				case "add":
				case "new":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					RunAddMode(args_queue.ToList());
					break;

				case "set":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					RunUpdateMode(args_queue.ToList(), true);
					break;

				case "change":
				case "update":
				case "upd":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					RunUpdateMode(args_queue.ToList(), false);
					break;

				case "rem":
				case "rm":
				case "remove":
				case "del":
				case "delete":
				case "unset":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					if (args_queue.Count == 0) throw new HostNotSpecifiedException();
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
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					if (args_queue.Count == 0) throw new HostNotSpecifiedException();
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
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					if (args_queue.Count == 0) throw new HostNotSpecifiedException();
					Lines = Hosts.GetMatched(args[1]);
					if (Lines.Count == 0) throw new HostNotFoundException(args[1]);
					foreach (HostsItem Line in Lines)
					{
						Line.Enabled = false;
						Console.WriteLine("[DISABLED] {0} {1}", Line.IP.ToString(), Line.Aliases.ToString());
					}
					break;

				case "hide":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					if (args_queue.Count == 0) throw new HostNotSpecifiedException();
					Lines = Hosts.GetMatched(args[1]);
					if (Lines.Count == 0) throw new HostNotFoundException(args[1]);
					foreach (HostsItem Line in Lines)
					{
						Line.Hidden = true;
						Console.WriteLine("[HIDDEN] {0} {1}", Line.IP.ToString(), Line.Aliases.ToString());
					}
					break;

				case "unhide":
					if (!MakeWritable(hosts_file)) throw new NoWritePermissionException();
					if (args_queue.Count == 0) throw new HostNotSpecifiedException();
					Lines = Hosts.GetMatched(args[1]);
					if (Lines.Count == 0) throw new HostNotFoundException(args[1]);
					foreach (HostsItem Line in Lines)
					{
						Line.Hidden = false;
						Console.WriteLine("[UNHIDDEN] {0} {1}", Line.IP.ToString(), Line.Aliases.ToString());
					}
					break;

				default:
					Console.WriteLine($"[ERROR] Unknown command '{mode}'.\n");
					Help();
					return false;
			}

			MakeWritable(rollback_file);
			ForceCopy(hosts_file, rollback_file);
			Hosts.Save();
			return true;
		}
		catch (NoWritePermissionException)
		{
			Console.WriteLine("[ERROR] No write permission to modify the hosts file.");
			return false;
		}
		catch (HostNotSpecifiedException)
		{
			Console.WriteLine("[ERROR] Host is not specified.");
			return false;
		}
		catch (HostNotFoundException e)
		{
			Console.WriteLine("[ERROR] Host '{0}' is not found.", e.Host);
			return false;
		}
		catch (Exception e)
		{
#if DEBUG
			Console.WriteLine("[ERROR] " + e.ToString());
#else
			Console.WriteLine("[ERROR] " + e.Message);
#endif
			return false;
		}
	}

	static void RunListMode(List<string> args)
	{
		bool? visible_only = true;
		bool? enabled_only = true;
		string mask = null;
		var args_queue = new Queue<string>(args);

		while (args_queue.Count > 0)
		{
			string arg = args_queue.Dequeue().ToLower();
			if (arg == "--all")
			{
				visible_only = null;
				enabled_only = null;
			}
			else if (mask == null)
			{
				mask = arg;
				if (!mask.StartsWith("*") && !mask.EndsWith("*")) mask = '*' + mask + '*';
			}
			else
			{
				throw new Exception($"Unknown argument '{arg}'.");
			}
		}

		View(mask ?? "*", visible_only, enabled_only);
	}

	static void View(string mask, bool? visible_only = null, bool? enabled_only = null)
	{
		int enabled = 0;
		int disabled = 0;
		int hidden = 0;

		if (mask != "*") Console.WriteLine("Mask: {0}\n", mask);

		Hosts.RemoveInvalid();
		Hosts.ResetFormat();
		List<HostsItem> found_lines = Hosts.GetMatched(mask);
		foreach (HostsItem line in found_lines)
		{
			if (line.Enabled) enabled++; else disabled++;
			if (line.Hidden) hidden++;
			if (visible_only != null && visible_only.Value == line.Hidden) continue;
			if (enabled_only != null && enabled_only.Value != line.Enabled) continue;
			Console.WriteLine(line);
		}
		if (found_lines.Count > 0) Console.WriteLine();
		Console.WriteLine("Enabled: {0,-4} Disabled: {1,-4} Hidden: {2,-4}", enabled, disabled, hidden);
	}

	static void AddHostsItem(NetAddress address, HostAliases aliases, string comment)
	{
		if (aliases.Count == 0) throw new HostNotSpecifiedException();

		// Remove duplicates
		var lines = Hosts.GetValid().FindAll(item => item.IP.Type == address.Type && item.Aliases.ContainsAny(aliases));
		foreach (var line in lines)
		{
			if (!line.Aliases.Except(aliases).Any())
			{
				Hosts.Remove(line);
				Console.WriteLine("[REMOVED] {0} {1}", line.IP.ToString(), line.Aliases.ToString());
			}
			else
			{
				line.Aliases.RemoveAll(aliases);
				Console.WriteLine("[UPDATED] {0} {1}", line.IP.ToString(), line.Aliases.ToString());
			}
		}

		// New host
		var new_item = new HostsItem(address, aliases, comment == null ? "" : comment.Trim());
		Hosts.Add(new_item);
		Console.WriteLine("[ADDED] {0} {1}", new_item.IP.ToString(), new_item.Aliases.ToString());
	}

	static void RunAddMode(List<string> args)
	{
		if (args.Count == 0) throw new HostNotSpecifiedException();

		HostAliases aliases = new HostAliases();
		NetAddress address_ipv4 = null;
		NetAddress address_ipv6 = null;
		string comment = "";
		bool in_comment = false;

		var args_queue = new Queue<string>(args);
		while (args_queue.Count > 0)
		{
			string arg = args_queue.Dequeue();
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
				{
					if (address_ipv4 != null) { throw new Exception("More than one IPv4 address is not allowed."); }
					address_ipv4 = address_test;
				}
				else
				{
					if (address_ipv6 != null) { throw new Exception("More than one IPv6 address is not allowed."); }
					address_ipv6 = address_test;
				}
				continue;
			}
			var hostname_test = HostName.TryCreate(arg);
			if (hostname_test != null)
			{
				aliases.Add(hostname_test);
				continue;
			}
			throw new Exception($"Unknown argument '{arg}'.");
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

	static void RunUpdateMode(List<string> args, bool autoadd = false)
	{
		if (args.Count == 0) throw new HostNotSpecifiedException();

		var args_queue = new Queue<string>(args);
		string mask = args_queue.Dequeue();
		List<HostsItem> lines = Hosts.GetMatched(mask);
		if (lines.Count == 0 && (!autoadd || mask.IndexOf('*') != -1))
		{
			throw new HostNotFoundException(mask);
		}

		NetAddress address_ipv4 = null;
		NetAddress address_ipv6 = null;
		string comment = null;
		bool in_comment = false;

		while (args_queue.Count > 0)
		{
			string arg = args_queue.Dequeue();
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
				{
					if (address_ipv4 != null) { throw new Exception("More than one IPv4 address is not allowed."); }
					address_ipv4 = address_test;
				}
				else
				{
					if (address_ipv6 != null) { throw new Exception("More than one IPv6 address is not allowed."); }
					address_ipv6 = address_test;
				}
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
		Hosts = new HostsEditor(GetHostsFileName());

		try
		{
			// Create default hosts file if not exists
			var hosts_file = Hosts.FileName;
			if (!File.Exists(hosts_file))
			{
				File.WriteAllText(hosts_file, new HostsItem("127.0.0.1", "localhost").ToString());
			}

			// Try to create backup on first run
			var backup_file = hosts_file + ".backup";
			if (!File.Exists(backup_file))
			{
				ForceCopy(hosts_file, backup_file);
			}
		}
		catch
		{
			throw new Exception("The hosts file does not exist.");
		}
	}

	static int Main(string[] args)
	{
		try
		{
			Init();

			if (args.Length > 0 && args[0].ToLower() != "shell")
			{
				return Execute(args.ToList()) ? 0 : 1;
			}
			else
			{
				Console.WriteLine(GetTitle());
				Console.WriteLine(GetCopyright());
				Console.WriteLine("Hosts file: " + Hosts.FileName.ToLower());
				Console.WriteLine();

				IsShell = true;
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
					Execute(command.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList());
					Console.WriteLine();
				}

				return 0;
			}
		}
		catch (Exception e)
		{
#if DEBUG
			Console.WriteLine("[ERROR] " + e.ToString());
#else
			Console.WriteLine("[ERROR] " + e.Message);
#endif
			return 1;
		}
	}
}
