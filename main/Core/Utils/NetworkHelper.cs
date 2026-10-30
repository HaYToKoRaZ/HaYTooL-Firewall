using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GuvenlikDuvarim.Core.Utils
{
    public static class NetworkHelper
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder, int ulAf, int TableClass, uint Reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize, bool bOrder, int ulAf, int TableClass, uint Reserved);

        private const int AF_INET = 2; // IPv4
        private const int AF_INET6 = 23; // IPv6
        private const int TCP_TABLE_OWNER_PID_ALL = 5;
        private const int UDP_TABLE_OWNER_PID_ALL = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint dwState;
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwRemoteAddr;
            public uint dwRemotePort;
            public uint dwOwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwOwningPid;
        }

        public static Dictionary<int, int> GetActiveConnectionCounts()
        {
            var pidCounts = new Dictionary<int, int>();

            try
            {
                FetchTcpTable(AF_INET, pidCounts);
                FetchTcpTable(AF_INET6, pidCounts);
                FetchUdpTable(AF_INET, pidCounts);
                FetchUdpTable(AF_INET6, pidCounts);
            }
            catch { }

            return pidCounts;
        }

        private static void FetchTcpTable(int family, Dictionary<int, int> pidCounts)
        {
            int size = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref size, false, family, TCP_TABLE_OWNER_PID_ALL, 0);
            if (size == 0) return;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                if (GetExtendedTcpTable(buffer, ref size, false, family, TCP_TABLE_OWNER_PID_ALL, 0) == 0)
                {
                    int numEntries = Marshal.ReadInt32(buffer);
                    IntPtr rowPtr = IntPtr.Add(buffer, 4);

                    for (int i = 0; i < numEntries; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                        int pid = (int)row.dwOwningPid;
                        if (pid > 0)
                        {
                            pidCounts[pid] = pidCounts.GetValueOrDefault(pid, 0) + 1;
                        }
                        rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<MIB_TCPROW_OWNER_PID>());
                    }
                }
            }
            catch { }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static void FetchUdpTable(int family, Dictionary<int, int> pidCounts)
        {
            int size = 0;
            GetExtendedUdpTable(IntPtr.Zero, ref size, false, family, UDP_TABLE_OWNER_PID_ALL, 0);
            if (size == 0) return;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                if (GetExtendedUdpTable(buffer, ref size, false, family, UDP_TABLE_OWNER_PID_ALL, 0) == 0)
                {
                    int numEntries = Marshal.ReadInt32(buffer);
                    IntPtr rowPtr = IntPtr.Add(buffer, 4);

                    for (int i = 0; i < numEntries; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(rowPtr);
                        int pid = (int)row.dwOwningPid;
                        if (pid > 0)
                        {
                            pidCounts[pid] = pidCounts.GetValueOrDefault(pid, 0) + 1;
                        }
                        rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<MIB_UDPROW_OWNER_PID>());
                    }
                }
            }
            catch { }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
