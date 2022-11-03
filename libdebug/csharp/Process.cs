using System.Runtime.InteropServices;

namespace libdebug
{
    public class Process
    {
        public string Name;
        public int Pid;

        /// <summary>
        /// Initializes Process class
        /// </summary>
        /// <param name="name">Process name</param>
        /// <param name="pid">Process ID</param>
        /// <returns></returns>
        public Process(string name, int pid)
        {
            this.Name = name;
            this.Pid = pid;
        }
        public override string ToString()
        {
            return $"[{Pid}] {Name}";
        }
    }

    public class ProcessList
    {
        public Process[] Processes { get; private set; }

        /// <summary>
        /// Initializes ProcessList class
        /// </summary>
        /// <param name="number">Number of processes</param>
        /// <param name="names">Process names</param>
        /// <param name="pids">Process IDs</param>
        /// <returns></returns>
        public ProcessList(int number, string[] names, int[] pids)
        {
            Processes = new Process[number];
            for (int i = 0; i < number; i++)
            {
                Processes[i] = new Process(names[i], pids[i]);
            }
        }

        /// <summary>
        /// Finds a process based off name
        /// </summary>
        /// <param name="name">Process name</param>
        /// <param name="contains">Condition to check if process name contains name</param>
        /// <returns></returns>
        public Process FindProcess(string name, bool contains = false)
        {
            foreach (Process p in Processes)
            {
                if (contains)
                {
                    if (p.Name.Contains(name))
                    {
                        return p;
                    }
                }
                else
                {
                    if (p.Name == name)
                    {
                        return p;
                    }
                }
            }

            return null;
        }
    }

    public class MemoryEntry
    {
        public string Name;
        public ulong Start;
        public ulong End;
        public ulong Offset;
        public uint Prot;
    }

    public class ProcessMap
    {
        public int Pid { get; }
        public MemoryEntry[] Entries { get; private set; }

        /// <summary>
        /// Initializes ProcessMap class with memory entries and process ID
        /// </summary>
        /// <param name="pid">Process ID</param>
        /// <param name="entries">Process memory entries</param>
        /// <returns></returns>
        public ProcessMap(int pid, MemoryEntry[] entries)
        {
            this.Pid = pid;
            this.Entries = entries;
        }

        /// <summary>
        /// Finds a virtual memory entry based off name
        /// </summary>
        /// <param name="name">Virtual memory entry name</param>
        /// <param name="contains">Condition to check if entry name contains name</param>
        /// <returns></returns>
        public MemoryEntry FindEntry(string name, bool contains = false)
        {
            foreach (MemoryEntry entry in Entries)
            {
                if (contains)
                {
                    if (entry.Name.Contains(name))
                    {
                        return entry;
                    }
                }
                else
                {
                    if (entry.Name == name)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a virtual memory entry based off size
        /// </summary>
        /// <param name="size">Virtual memory entry size</param>
        /// <returns></returns>
        public MemoryEntry FindEntry(ulong size)
        {
            foreach (MemoryEntry entry in Entries)
            {
                if ((entry.Start - entry.End) == size)
                {
                    return entry;
                }
            }

            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct ProcessInfo
    {
        public int Pid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Path;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string TitleId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string ContentId;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct ThreadInfo
    {
        public int Pid;
        public int Priority;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Name;
    }
}