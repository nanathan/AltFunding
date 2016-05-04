using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AltFunding
{
    public class Calendar
    {
        private static double time { get { return Planetarium.GetUniversalTime(); } }
        private const int SecondsPerMinute = 60;
        private const int SecondsPerHour = SecondsPerMinute * 60;
        private const int SecondsPerDay = SecondsPerHour * 6;

        public static Date Now { get { return FromUT(Planetarium.GetUniversalTime()); } }

        public static Date FromUT(double ut)
        {
            double t = ut;
            int secondsPerYear = (int) Math.Floor(Planetarium.fetch.Home.orbit.period);

            int year = (int) Math.Floor(t / secondsPerYear);
            t -= year * secondsPerYear;

            int dayOfYear = (int) Math.Floor(t / SecondsPerDay);
            t -= dayOfYear * SecondsPerDay;

            int hour = (int) Math.Floor(t / SecondsPerHour);
            t -= hour * SecondsPerHour;

            int minute = (int) Math.Floor(t / SecondsPerMinute);
            t -= minute * SecondsPerMinute;

            int month = dayOfYear / 30;
            int day = dayOfYear - month * 30;

            return new Date(year + 1, month + 1, day + 1, dayOfYear + 1, hour, minute, (int) t, ut);
        }
    }

    public class Date
    {
        public int Year { get; private set; }
        public int Month { get; private set; }
        public int Day { get; private set; }
        public int DayOfYear { get; private set; }
        public int Hour { get; private set; }
        public int Minute { get; private set; }
        public int Second { get; private set; }
        public double UT { get; private set; }

        internal Date(int year, int month, int day, int dayOfYear, int hour, int minute, int second, double ut)
        {
            this.Year = year;
            this.Month = month;
            this.Day = day;
            this.DayOfYear = dayOfYear;
            this.Hour = hour;
            this.Minute = minute;
            this.Second = second;
            this.UT = ut;
        }
    }
}
