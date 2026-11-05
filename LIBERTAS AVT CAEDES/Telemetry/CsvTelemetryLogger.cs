using System.Globalization;
using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Telemetry
{
    public sealed class CsvTelemetryLogger : IDisposable
    {
        public Guid AnonymTesterID { get; } = Guid.NewGuid();
        public bool HasConsented { get; private set; }
        private StreamWriter? _writer;
        private readonly string _path;

        public CsvTelemetryLogger(string filePath = "telemetry_sessions.csv")
        {
            _path = filePath;
            TelemetryBus.OnEventPublished += WriteRecord;
        }

        public void SetConsent(bool consent)
        {
            HasConsented = consent;
            if (HasConsented)
            {
                _writer = new(_path, append: true) { AutoFlush = true };
                if (!File.Exists(_path))
                    _writer.WriteLine("SessionGUID,TimestampISO,Stage,EventType,PosX,PosZ,ExtraParams");
                TelemetryBus.Publish(0, TelemetryEventType.SessionConsent, Vector3.Zero, "Consent=Granted");
            }
            else return;
        }

        private void WriteRecord(TelemetryEvent t)
        {
            if (!HasConsented || _writer is null) return;

            string
                timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                posX = t.Position.X.ToString("F2", CultureInfo.InvariantCulture),
                posY = t.Position.Y.ToString("F2", CultureInfo.InvariantCulture),
                posZ = t.Position.Z.ToString("F2", CultureInfo.InvariantCulture),
                safeParams = $"\"{t.Parameters.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ")}\"";

            _writer.WriteLine($"{AnonymTesterID},{timestamp},{t.Stage},{t.EventType},{posX},{posY},{posZ},{safeParams}");
        }

        public void Dispose()
        {
            TelemetryBus.OnEventPublished -= WriteRecord;
            _writer?.Dispose();
        }
    }
}
