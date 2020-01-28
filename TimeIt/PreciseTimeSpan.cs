using System;

namespace TimeIt
{
    internal struct PreciseTimeSpan
    {
        public int Hours;
        public int Minutes;
        public int Seconds;
        public int Milliseconds;
        public int Nanoseconds;

        private static ValueTuple<decimal, decimal> FloorWithRemainder(decimal value)
        {
            decimal flooredValue = Math.Floor(value);
            decimal remainder = (value - flooredValue);
            return new ValueTuple<decimal, decimal>(flooredValue, remainder);
        }

        internal static PreciseTimeSpan FromTicks(long ticks)
        {
            if (ticks < 0)
            {
                throw new ArgumentException("Ticks must be positive value.");
            }

            PreciseTimeSpan ts = new PreciseTimeSpan();

            decimal totalNanoseconds = (ulong)ticks * 100m;

            decimal hoursPart = ((((totalNanoseconds / 1_000_000.0m) / 1_000.0m) / 60.0m) / 60.0m);
            (decimal flooredHours, decimal hourRemainder) = FloorWithRemainder(hoursPart);
            ts.Hours = (int)flooredHours;

            decimal minutesPart = hourRemainder * 60.0m;
            (decimal flooredMinutes, decimal minuteRemainder) = FloorWithRemainder(minutesPart);
            ts.Minutes = (int)flooredMinutes;

            decimal secondPart = minuteRemainder * 60.0m;
            (decimal flooredSeconds, decimal secondRemainder) = FloorWithRemainder(secondPart);
            ts.Seconds = (int)flooredSeconds;

            decimal millisecondPart = secondRemainder * 1000.0m;
            (decimal flooredMs, decimal msRemainder) = FloorWithRemainder(millisecondPart);
            ts.Milliseconds = (int)flooredMs;

            decimal nanosecondPart = msRemainder * 1_000_000.0m;
            ts.Nanoseconds = (int)nanosecondPart;
            
            return ts;
        }

    }
}
