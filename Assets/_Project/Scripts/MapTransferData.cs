public static class MapTransferData
{
    public static string TargetSpawnPointName { get; private set; }

    public static void SetTarget(string targetSpawnPointName)
    {
        TargetSpawnPointName = targetSpawnPointName;
    }

    public static void Clear()
    {
        TargetSpawnPointName = null;
    }
}
