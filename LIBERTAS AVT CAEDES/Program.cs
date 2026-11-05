using LIBERTAS_AVT_CAEDES.Core;

namespace LIBERTAS_AVT_CAEDES
{
    public static class Program
    {
        public static void Main()
        {
            using GameEngine engine = new();
            engine.Run();
        }
    }
}