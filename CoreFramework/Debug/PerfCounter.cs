using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CoreFramework.Debugging
{
    /// <summary>
    /// Класс, повзволяющий точно замерять промежутки времени
    /// </summary>
    /// <remarks></remarks>
    [Obsolete("Use System.Diagnostics.Stopwatch")]
    public class PerfCounter
    {
        private static long? _freq;
        private long _start;
        [DllImport("Kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        /// <summary>
        /// The QueryPerformanceCounter function retrieves the current value of the high-resolution performance counter
        /// </summary>
        /// <param name="X">Variable that receives the current performance-counter value, in counts</param>
        /// <returns>If the function succeeds, the return value is <b>true</b></returns>
        /// <remarks>Делегация системному вызову</remarks>
        public static extern bool QueryPerformanceCounter(ref long X);
        [DllImport("Kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        /// <summary>
        /// The QueryPerformanceFrequency function retrieves the frequency of the high-resolution performance counter, if one exists. The frequency cannot change while the system is running
        /// </summary>
        /// <param name="X">variable that receives the current performance-counter frequency, in counts per second. If the installed hardware does not support a high-resolution performance counter, this parameter can be zero.</param>
        /// <returns>If the function succeeds, the return value is <b>true</b></returns>
        /// <remarks>Делегация системному вызову</remarks>
        public static extern bool QueryPerformanceFrequency(ref long X);

        /// <summary>
        /// Констуктор
        /// </summary>
        /// <remarks>Начала отсчета</remarks>
        public PerfCounter(bool autoStart = true)
        {
            if (!_freq.HasValue)
            {
                long f = 0;
                if (QueryPerformanceFrequency(ref f))
                    _freq = f;
            }
            
            if (autoStart)
                Start();
        }
        public void Start()
        {
            QueryPerformanceCounter(ref _start);
        }
        /// <summary>
        /// Функция окончания отсчета времени
        /// </summary>
        /// <returns>Временой промежуток прошедщий с момента создания данного экземпляра</returns>
        /// <remarks></remarks>
        public TimeSpan GetTime()
        {
            if (_freq.HasValue)
            {
                long end = 0;
                QueryPerformanceCounter(ref end);
                return TimeSpan.FromSeconds(((double)end - _start) / _freq.Value);
            }
            else
                return TimeSpan.MinValue;
        }
    }
}