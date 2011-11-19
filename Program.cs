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
	class HostNotSpecifiedException : ApplicationException { }
	class HostNotFoundException : ApplicationException 
	{
		public string Host { get; protected set; }
		public HostNotFoundException(string host) { Host = host; }
	}

	class Program
	{
		static bool IsUnix;

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
				return Environment.ExpandEnvironmentVariables(HostsPath + @"\hosts");
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

		static string GetCopyright()
		{
			var CopyrightAttribute = GetAssemblyAttribute<AssemblyCopyrightAttribute>();
			return (CopyrightAttribute == null) ? String.Empty : CopyrightAttribute.Copyright;
		}

		static string GetTitle()
		{
			var TitleAttribute = GetAssemblyAttribute<AssemblyTitleAttribute>();
			return (TitleAttribute == null) ? String.Empty : TitleAttribute.Title;
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
				Console.WriteLine();
			}
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
			Console.WriteLine("  recreate   - empty hosts file");
			if (!IsUnix)
			{
				Console.WriteLine("  open       - open hosts file in notepad");
			}
			if (interactive)
			{
				Console.WriteLine("  exit       - exit from command interpreter");
			}
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
			if(FoundLines.Count > 0) Console.WriteLine();
			Console.WriteLine("Enabled: {0,-4} Disabled: {1,-4} Hidden: {2,-4}", enabled, disabled, hidden);
		}

		static void Run(string[] args, bool interactive)
		{
			try
			{
				ArgsQueue = new Queue<string>(args);
				string Mode = (ArgsQueue.Count > 0) ? ArgsQueue.Dequeue().ToLower() : "help";
				string HostsFile = GetHostsFileName();
				string BackupHostsFile = HostsFile + ".backup";
				string RollbackHostsFile = HostsFile + ".rollback";

				// Check permissions
				FileIOPermission HostsPermissions = new FileIOPermission(FileIOPermissionAccess.AllAccess, HostsFile);
				if (!SecurityManager.IsGranted(HostsPermissions)) throw new Exception("No write permission to the hosts file");

				// Create default hosts file if not exists
				if (!File.Exists(HostsFile))
				{
					File.WriteAllText(HostsFile, new HostsItem("127.0.0.1", "localhost").ToString());
				}

				switch (Mode)
				{
					case "open":
						if (IsUnix) break;
						var exe = FileAssoc.GetExecutable(".txt") ?? "notepad";
						Process.Start(exe, '"' + HostsFile + '"');
						return;

					case "backup":
						if (ArgsQueue.Count > 0) BackupHostsFile = HostsFile + "." + ArgsQueue.Dequeue().ToLower();
						File.Copy(HostsFile, BackupHostsFile, true);
						Console.WriteLine("[OK] Hosts file backed up successfully");
						return;

					case "restore":
						if (ArgsQueue.Count > 0) BackupHostsFile = HostsFile + "." + ArgsQueue.Dequeue().ToLower();
						if (!File.Exists(BackupHostsFile)) throw new Exception("Backup file is not exists");
						File.Copy(HostsFile, RollbackHostsFile, true);
						File.Copy(BackupHostsFile, HostsFile, true);
						Console.WriteLine("[OK] Hosts file restored successfully");
						return;

					case "rollback":
						if (!File.Exists(RollbackHostsFile)) throw new Exception("Rollback file is not exists");
						if (File.Exists(HostsFile)) File.Delete(HostsFile);
						File.Move(RollbackHostsFile, HostsFile);
						Console.WriteLine("[OK] Hosts file rolled back successfully");
						return;

					case "recreate":
						File.Copy(HostsFile, RollbackHostsFile, true);
						File.WriteAllText(HostsFile, new HostsItem("127.0.0.1", "localhost").ToString());
						Console.WriteLine("[OK] New hosts file created successfully");
						return;

					case "help":
						Help(interactive);
						return;
				}

				// Try to create backup on first run
				if (!File.Exists(BackupHostsFile))
				{
					try
					{
						File.Copy(HostsFile, BackupHostsFile);
					}
					catch {}
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
						RunListMode(interactive);
						return;

					case "format":
						Hosts.ResetFormat();
						Console.WriteLine("[OK] Hosts file formatted successfully");
						break;

					case "clean":
						Hosts.RemoveInvalid();
						Hosts.ResetFormat();
						Console.WriteLine("[OK] Hosts file cleaned successfully");
						break;

					case "add":
					case "new":
						RunAddMode();
						break;

					case "set":
					case "change":
					case "update":
						RunUpdateMode();
						break;

					case "rem":
					case "rm":
					case "remove":
					case "del":
					case "delete":
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
						Console.WriteLine();
						Help(interactive);
						return;
				}
				File.Copy(HostsFile, RollbackHostsFile, true);
				Hosts.Save();
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
				Console.WriteLine("[ERROR] " + e.Message);
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

		static void RunAddMode()
		{
			if (ArgsQueue.Count == 0) throw new HostNotSpecifiedException();
			HostAliases aliases = new HostAliases();
			NetAddress address = new NetAddress("127.0.0.1");
			string comment = "";
			bool inComment = false;

			while (ArgsQueue.Count > 0)
			{
				string arg = ArgsQueue.Dequeue();
				if (inComment)
				{
					comment += arg + " ";
					continue;
				}
				if (arg.Length > 0 && arg[0] == '#')
				{
					inComment = true;
					comment = (arg.Length > 1) ? (arg.Substring(1) + " ") : "";
					continue;
				}
				arg = arg.ToLower();
				NetAddress TestAddress = NetAddress.TryCreate(arg);
				if (TestAddress != null)
				{
					address = TestAddress;
					continue;
				}
				HostName TestHostName = HostName.TryCreate(arg);
				if (TestHostName != null)
				{
					aliases.Add(TestHostName);
					continue;
				}
				throw new Exception(String.Format("Unknown argument '{0}'", arg));
			}

			HostsItem line = new HostsItem(address, aliases, comment);
			Hosts.Add(line);
			Console.WriteLine("[ADDED] {0} {1}", line.IP.ToString(), line.Aliases.ToString());
		}

		static void RunUpdateMode()
		{
			if (ArgsQueue.Count == 0) throw new HostNotSpecifiedException();
			string mask = ArgsQueue.Dequeue();
			List<HostsItem> lines = Hosts.GetMatched(mask);
			if (lines.Count == 0) throw new HostNotFoundException(mask);
			NetAddress address = null;
			string comment = null;
			bool inComment = false;

			while (ArgsQueue.Count > 0)
			{
				string arg = ArgsQueue.Dequeue();
				if (inComment)
				{
					comment += arg + " ";
					continue;
				}
				if (arg.Length > 0 && arg[0] == '#')
				{
					inComment = true;
					comment = (arg.Length > 1) ? (arg.Substring(1) + " ") : "";
					continue;
				}
				arg = arg.ToLower();
				NetAddress TestAddress = NetAddress.TryCreate(arg);
				if (TestAddress != null)
				{
					address = TestAddress;
					continue;
				}
			}

			foreach (HostsItem line in lines)
			{
				if (address != null) line.IP = address;
				if (comment != null) line.Comment = comment;
				Console.WriteLine("[UPDATED] {0} {1}", line.IP.ToString(), line.Aliases.ToString());
			}
		}

		static void Main(string[] args)
		{
			try
			{
				IsUnix = (Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX);
				if (args.Length > 0)
				{
					Run(args, false);
				}
				else
				{
					Console.WriteLine(GetTitle());
					Console.WriteLine(GetCopyright());
					Console.WriteLine("Hosts file: " + GetHostsFileName().ToLower());
					Console.WriteLine();
					while (true)
					{
						Console.Write("hosts> ");
						var command = Console.ReadLine().Replace("\0", "").Trim();
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
				Console.WriteLine("[ERROR] " + e.Message);
			}
		}
	}
}
