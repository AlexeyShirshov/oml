using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;

namespace Tests
{
    internal class HiPerfTimer
    {
        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        private long startTime, stopTime;
        private long freq;
        public static bool run = true;

        public HiPerfTimer()
        {
            if (QueryPerformanceFrequency(out freq) == false)
            {
                throw new Win32Exception();
            }
        }

        public void Start()
        {
            Thread.Sleep(0);
            QueryPerformanceCounter(out startTime);
        }

        public void Stop()
        {
            QueryPerformanceCounter(out stopTime);
        }

        public double Duration
        {
            get
            {
                return (double)(stopTime - startTime) / (double)freq;
            }
        }
    }
}
