using UnityEngine;

public static class GameplayInputBlocker
{
    private static int blockCount;

    public static bool IsBlocked => blockCount > 0;

    public static void AcquireBlock()
    {
        blockCount++;
    }

    public static void ReleaseBlock()
    {
        blockCount = Mathf.Max(0, blockCount - 1);
    }
}
