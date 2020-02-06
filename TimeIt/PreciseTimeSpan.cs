using System;

namespace TimeIt
{
    /// <summary>
    /// TimeSpan clone with higher precision.
    /// </summary>
    internal readonly struct PreciseTimeSpan
    {
        /// <summary>
        /// Original ticks.
        /// </summary>
        public readonly long Ticks;

        /// <summary>
        /// Hours part.
        /// </summary>
        public readonly int Hours;

        /// <summary>
        /// Minutes part.
        /// </summary>
        public readonly int Minutes;

        /// <summary>
        /// Seconds part.
        /// </summary>
        public readonly int Seconds;

        /// <summary>
        /// Milliseconds part.
        /// </summary>
        public readonly int Milliseconds;

        /// <summary>
        /// Nanoseconds part.
        /// </summary>
        public readonly int Nanoseconds;

        public static PreciseTimeSpan Zero => new PreciseTimeSpan(0, 0, 0, 0, 0, 0);


        private PreciseTimeSpan(long ticks, int hours, int minutes, int seconds, int milliseconds, int nanoseconds)
        {
            Ticks = ticks;
            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
            Milliseconds = milliseconds;
            Nanoseconds = nanoseconds;
        }

        /// <summary>
        /// Construct <see cref="PreciseTimeSpan"/> from 100 ns ticks.
        /// </summary>
        /// <param name="ticks">Number of 100 ns ticks.</param>
        /// <exception cref="System.ArgumentException">Thrown for invalid ticks value.</exception>
        /// <returns></returns>
        internal static PreciseTimeSpan FromTicks(long ticks)
        {
            if (ticks < 0)
            {
                throw new ArgumentException("Ticks must be positive value.");
            }

            decimal totalNanoseconds = (ulong)ticks * 100m;

            decimal hoursPart = ((((totalNanoseconds / 1_000_000.0m) / 1_000.0m) / 60.0m) / 60.0m);
            (decimal flooredHours, decimal hourRemainder) = FloorWithRemainder(hoursPart);

            decimal minutesPart = hourRemainder * 60.0m;
            (decimal flooredMinutes, decimal minuteRemainder) = FloorWithRemainder(minutesPart);

            decimal secondPart = minuteRemainder * 60.0m;
            (decimal flooredSeconds, decimal secondRemainder) = FloorWithRemainder(secondPart);

            decimal millisecondPart = secondRemainder * 1000.0m;
            (decimal flooredMs, decimal msRemainder) = FloorWithRemainder(millisecondPart);

            decimal nanosecondPart = msRemainder * 1_000_000.0m;

            return new PreciseTimeSpan(ticks, (int)flooredHours, (int)flooredMinutes, (int)flooredSeconds, (int)flooredMs, (int)nanosecondPart);
        }

        /// <summary>
        /// Helper function to return floored value and the remainder.
        /// </summary>
        /// <param name="value">Original value.</param>
        /// <returns>Tuple of floored value and remainder.</returns>
        private static ValueTuple<decimal, decimal> FloorWithRemainder(decimal value)
        {
            decimal flooredValue = Math.Floor(value);
            decimal remainder = (value - flooredValue);
            return new ValueTuple<decimal, decimal>(flooredValue, remainder);
        }

        /// <summary>
        /// Add ticks to PreciseTimeSpan.
        /// </summary>
        /// <param name="a">First PreciseTimeSpan.</param>
        /// <param name="ticks">Ticks</param>
        /// <returns>Result PreciseTimeSpan a.Ticks + ticks</returns>
        public static PreciseTimeSpan operator +(PreciseTimeSpan a, long ticks)
        {
            ulong totalTicks = (ulong)a.Ticks + (ulong)ticks;
            if (totalTicks > long.MaxValue)
            {
                throw new OverflowException("Too big timespan.");
            }
            return FromTicks((long)totalTicks);
        }

        /// <summary>
        /// Substract ticks from PreciseTimeSpan.
        /// </summary>
        /// <param name="a">First PreciseTimeSpan.</param>
        /// <param name="ticks">Ticks</param>
        /// <returns>Result PreciseTimeSpan a.Ticks - ticks</returns>
        public static PreciseTimeSpan operator -(PreciseTimeSpan a, long ticks)
        {
            long totalTicks = a.Ticks - ticks;
            if (totalTicks < 0)
            {
                throw new ArgumentException("Total tics are negative.");
            }
            return FromTicks(totalTicks);
        }

        /// <summary>
        /// Add two PreciseTimeSpans together.
        /// </summary>
        /// <param name="a">First PreciseTimeSpan.</param>
        /// <param name="b">Second PreciseTimeSpan.</param>
        /// <returns>Sum of both PreciseTimeSpans.</returns>
        public static PreciseTimeSpan operator +(PreciseTimeSpan a, PreciseTimeSpan b) => (a + b.Ticks);

        /// <summary>
        /// Substract PreciseTimeSpan from PreciseTimeSpan.
        /// </summary>
        /// <param name="a">Base PreciseTimeSpan.</param>
        /// <param name="b">PreciseTimeSpan to substract.</param>
        /// <returns>Result of substracting a - b</returns>
        public static PreciseTimeSpan operator -(PreciseTimeSpan a, PreciseTimeSpan b) => (a - b.Ticks);

        /// <summary>
        /// Add TimeSpan to PreciseTimeSpans.
        /// </summary>
        /// <param name="a">Base PreciseTimeSpan.</param>
        /// <param name="b">TimeSpan to add.</param>
        public static PreciseTimeSpan operator +(PreciseTimeSpan a, TimeSpan b) => (a + b.Ticks);

        /// <summary>
        /// Substract PreciseTimeSpan from PreciseTimeSpan.
        /// </summary>
        /// <param name="a">Base PreciseTimeSpan.</param>
        /// <param name="b">TimeSpan to substract.</param>
        public static PreciseTimeSpan operator -(PreciseTimeSpan a, TimeSpan b) => (a - b.Ticks);

    }
}
