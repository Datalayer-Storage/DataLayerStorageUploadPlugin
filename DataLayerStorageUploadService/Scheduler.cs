using System.Threading;
using System;

class Scheduler
{
    public static void ScheduleFunctionExecution(DayOfWeek targetDayOfWeek, TimeSpan targetTime, Action function)
    {
        DateTime now = DateTime.Now;
        DateTime nextWeekday = now.AddDays((7 + (int)targetDayOfWeek - (int)now.DayOfWeek) % 7).Date;
        DateTime targetDateTime = nextWeekday.Add(targetTime);
        TimeSpan delay = targetDateTime - now;

        Timer timer = null;
        timer = new Timer(_ =>
        {
            function();

            // Schedule the timer to run again next week
            ScheduleFunctionExecution(targetDayOfWeek, targetTime, function);

            // Dispose the timer after it has completed execution
            timer.Dispose();
        }, null, delay, Timeout.InfiniteTimeSpan);
    }
}