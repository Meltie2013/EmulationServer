
namespace EmulationServer.RealmServer.Realms;

public static class RealmPopulationCalculator
{
    public static float Calculate(int activeConnections, int maxConnections)
    {
        if (activeConnections <= 0 || maxConnections <= 0)
        {
            return 0.0f;
        }

        float population = (float)activeConnections / maxConnections * 2.0f;

        return Math.Clamp(population, 0.0f, 2.0f);
    }
}
