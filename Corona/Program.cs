using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Corona
{
    class Program
    {
        private const int max = 25_000_000;

        static async Task Main(string[] args)
        {
            var start = 5683l;
            var rates = new float[] { 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f };

            var date = DateTime.UtcNow.Date;
            var maxDate = new DateTime(2020, 10, 1);
            var days = (int)Math.Ceiling((maxDate - date).TotalDays);
            var today = new DataPoint()
            {
                Date = date,
                Values = rates.Select(r => (r, start)).ToArray()
            };

            var points = new List<DataPoint>(new[] { today });
            for (var ix = 0; ix < days; ix++)
            {
                var last = points.Last();
                points.Add(
                    new DataPoint()
                    {
                        Date = last.Date.AddDays(1),
                        Values = last.Values.Select(v =>
                        {
                            var value = (long)Math.Ceiling(v.rate * v.value);
                            value = value > max ? max : value;

                            return (v.rate, value);
                        }).ToArray()
                    });
            }

            var aggs = new List<DataPoint>();
            var tier = 1;
            var items = 0;
            var point = default(DataPoint?);
            for (var ix = 0; ix < points.Count; ix++)
            {
                tier = (ix / 10) + 1;

                var targetCount = ((tier - 1) * 2);
                targetCount = targetCount == 0 ? 1 : targetCount;
                targetCount = targetCount > 10 ? 10 : targetCount;
                if (!point.HasValue)
                {
                    point = points[ix];
                }
                else
                {
                    var current = points[ix];
                    point = new DataPoint()
                    {
                        Date = point.Value.Date,
                        Values = point.Value.Values.Select(v =>
                        {
                            var rateValue = current.Values.First(v1 => v.rate == v1.rate);
                            var newValue = v.value + rateValue.value;
                            newValue = newValue > max ? max : newValue;
                            return (v.rate, newValue);
                        }).ToArray()
                    };
                }

                items++;

                if (items == targetCount || ix == points.Count - 1)
                {
                    aggs.Add(point.Value);
                    items = 0;
                    point = null;
                }


            }

            await SaveAsync(rates, aggs);
        }

        private static async Task SaveAsync(float[] rates, List<DataPoint> points)
        {
            using (var sw = new StreamWriter(File.Open("output.csv", FileMode.Create)))
            {
                await sw.WriteLineAsync($"Date, {string.Join(",", rates.Select(r => "r " + r))}");
                foreach (var dp in points)
                    await sw.WriteLineAsync(dp.ToString());
            }
        }

        struct DataPoint
        {
            public DateTime Date { get; set; }

            public (float rate, long value)[] Values { get; set; }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append(Date.ToString("dd.MM.yyyy"));
                builder.Append(",");
                builder.Append(string.Join(",", Values.Select(v => v.value)));
                return builder.ToString();
            }
        }
    }
}
