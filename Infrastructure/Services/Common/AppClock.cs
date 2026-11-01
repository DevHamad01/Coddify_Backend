using Application.Interfaces.Common;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services.Common
{
    public class AppClock : IAppClock
    {
        private readonly TimeZoneInfo _zone;

        public AppClock(IConfiguration config)
        {
            var id = config["App:TimeZone"] ?? "Asia/Karachi";
            _zone = TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) ? zone : TimeZoneInfo.Utc;
        }

        public DateTime UtcNow => DateTime.UtcNow;

        private DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zone);

        public DateOnly Today => DateOnly.FromDateTime(LocalNow);

        public DateOnly WeekStart
        {
            get
            {
                var today = Today;
                var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
                return today.AddDays(-daysSinceMonday);
            }
        }

        public DateTime NextMidnightUtc => ToUtc(Today.AddDays(1).ToDateTime(TimeOnly.MinValue));

        public DateTime WeekEndsUtc => ToUtc(WeekStart.AddDays(6).ToDateTime(new TimeOnly(23, 59)));

        private DateTime ToUtc(DateTime local) =>
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), _zone);
    }
}
