using System;
using System.Collections.Generic;

public enum ExpeditionResult
{
    Escaped,
    Dead,
    MIA
}

[Serializable]
public sealed class ExpeditionResultData
{
    public ExpeditionResult Result;
    public float ElapsedTicks;
    public int ReachedFloor;
    public List<string> AcquiredItems = new List<string>();
}
