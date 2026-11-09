namespace LIBERTAS_AVT_CAEDES.Telemetry
{
    public class PlayerTelemetry
    {
        public static Guid SessionGuid { get; set; }
        public static bool ConsentGranted { get; set; }
        public static uint HostagesTransported { get; set; }
        public static uint EnemiesKilled { get; set; }
        public static uint AcolytesKilled { get; set; }
        public static uint ChimerasKilled { get; set; }
        public static uint PlayerBusted { get; set; }
        public static uint PlayerDied { get; set; }
        public static double DistanceTraversed { get; set; } // total distance of path traversed (measured in each 30s)
        public static uint AcquiredPickups { get; set; }
        public static double TotalDiscoveryRate { get; set; }
        public static ushort LevelsCompleted { get; set; }

        public static ushort SelfSurveyAggro { get; set; } = 3;
        public static ushort SelfSurveyStealth { get; set; } = 3;
        public static ushort SelfSurveyDiscovery { get; set; } = 3;

        public static double RealSurveyAggro { get; set; } = 0;
        public static double RealSurveyStealth { get; set; } = 0;
        public static double RealSurveyDiscovery { get; set; } = 0;


        public static void ResetSession()
        {
            SessionGuid = Guid.NewGuid();
            HostagesTransported = 0;
            EnemiesKilled = 0;
            PlayerBusted = 0;
            PlayerDied = 0;
            DistanceTraversed = 0;
            AcquiredPickups = 0;
            TotalDiscoveryRate = 0;
            SelfSurveyAggro = 3;
            SelfSurveyStealth = 3;
            SelfSurveyDiscovery = 3;
        }

        public static void ExportSessionCsv()
        {
            if (!ConsentGranted) return;
            else
            {
                using var writer = new StreamWriter($"telemetry_{SessionGuid}.csv", append: false);
                writer.WriteLine("HostagesTransported;EnemiesKilled;PlayerBusts;PlayerDeaths;TraversedDistance;AcquiredPickups;TotalDiscoveryRate;SelfAggroRating;SelfStealthRating;SelfDiscoveryRating;RealAggroRating;RealStealthRating;RealDiscoveryRating");
                writer.WriteLine($"{HostagesTransported};{EnemiesKilled};{PlayerBusted};{PlayerDied};{DistanceTraversed};{AcquiredPickups};{TotalDiscoveryRate:F2}%;{SelfSurveyAggro};{SelfSurveyStealth};{SelfSurveyDiscovery};{RealSurveyAggro:F3};{RealSurveyStealth:F3};{RealSurveyDiscovery:F3}");
                writer.Flush();
                writer.Dispose();
            }
        }
    }
}
