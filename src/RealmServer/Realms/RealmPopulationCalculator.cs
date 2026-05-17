
namespace EmulationServer.RealmServer.Realms;

public static class RealmPopulationCalculator
{
    public static float Calculate(int activeConnections, int capacityLimit)
    {
        if (activeConnections <= 0 || capacityLimit <= 0)
        {
            return 0.0f;
        }

        float population = (float)activeConnections / capacityLimit * 2.0f;

        return Math.Clamp(population, 0.0f, 2.0f);
    }
}
