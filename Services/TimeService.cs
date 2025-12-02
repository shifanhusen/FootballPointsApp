namespace FootballPointsApp.Services
{
    public class TimeService
    {
        private readonly TimeZoneInfo _timeZone;

        public TimeService(IConfiguration configuration)
        {
            var timeZoneId = configuration["AppTimeZone"] ?? "UTC";
            try
            {
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                _timeZone = TimeZoneInfo.Utc;
            }
        }

        public DateTime ToLocal(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _timeZone);
        }

        public DateTime ToUtc(DateTime localDateTime)
        {
            // If the kind is already UTC, return it.
            if (localDateTime.Kind == DateTimeKind.Utc) return localDateTime;

            // If it's unspecified, assume it's in the app's timezone
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, _timeZone);
        }

        public string GetTimeZoneName()
        {
            return _timeZone.DisplayName;
        }
    }
}