using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Telemetry
{
    public static class TelemetryBus
    {
        public static event Action<TelemetryEvent>? OnEventPublished;

        public static void Publish(int stage, TelemetryEventType type, Vector3 pos, string parameters) =>
            OnEventPublished?.Invoke(new(stage, type, pos, parameters));
    }
}
