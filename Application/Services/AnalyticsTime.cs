using System;

namespace LogLens.Application.Services
{
    public static class AnalyticsTime
    {
        public const int BucketMinutes = 5;
        public static readonly TimeSpan BucketSize = TimeSpan.FromMinutes(BucketMinutes);

        public static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value;
            }

            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }

        public static DateTime FloorToBucket(DateTime value)
        {
            var utc = NormalizeUtc(value);
            var ticks = utc.Ticks - (utc.Ticks % BucketSize.Ticks);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        public static DateTime WindowStart(DateTime nowUtc, int bucketsBack)
        {
            var bucketStart = FloorToBucket(nowUtc);
            return bucketStart.AddMinutes(-(BucketMinutes * bucketsBack));
        }
    }
}
