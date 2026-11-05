using System.Numerics;

namespace LIBERTAS_AVT_CAEDES.Telemetry
{
    public enum TelemetryEventType
    {
        SessionConsent, LevelStarted, HostagesTransported, AcolyteKilled,
        PlayerBustedAndAttacked, PlayerDied, DistanceTraversedIn30s,
        PickupAcquired, LabyrinthDiscoveryRate, LevelCompleted,
        PostGameSurveySubmitted
    }

    public readonly record struct TelemetryEvent(
        int Stage,
        TelemetryEventType EventType,
        Vector3 Position,
        string Parameters
    );
}
