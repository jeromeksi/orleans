using System.Globalization;

#nullable disable

#if CLUSTERING_ADONET
namespace Orleans.Clustering.AdoNet.Storage
#elif PERSISTENCE_ADONET
namespace Orleans.Persistence.AdoNet.Storage
#elif REMINDERS_ADONET
namespace Orleans.Reminders.AdoNet.Storage
#elif STREAMING_ADONET
namespace Orleans.Streaming.AdoNet.Storage
#elif GRAINDIRECTORY_ADONET
namespace Orleans.GrainDirectory.AdoNet.Storage
#elif TESTER_SQLUTILS
namespace Orleans.Tests.SqlUtils
#else
// No default namespace intentionally to cause compile errors if something is not defined
#endif
{
    /// <summary>
    /// Formats .NET types appropriately for database consumption in non-parameterized queries.
    /// </summary>
    internal class AdoNetFormatProvider: IFormatProvider
    {
        private readonly AdoNetFormatter formatter = new();

        /// <summary>
        /// Returns an instance of the formatter
        /// </summary>
        /// <param name="formatType">Requested format type</param>
        /// <returns></returns>
        public object GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? formatter : null;


        private class AdoNetFormatter: ICustomFormatter
        {
            public string Format(string format, object arg, IFormatProvider formatProvider)
            {

                return arg switch
                {
                    null => "NULL", //This null check applies also to Nullable<T> when T does not have value defined.
                    string s => "N'" + s.Replace("'", "''", StringComparison.Ordinal) + "'",
                    DateTime time => "'" + time.ToString("O") + "'",
                    DateTimeOffset offset => "'" + offset.ToString("O") + "'",
                    IFormattable formattable => formattable.ToString(format, CultureInfo.InvariantCulture),
                    _ => arg.ToString()
                } ?? string.Empty;
            }
        }
    }
}

#nullable restore
