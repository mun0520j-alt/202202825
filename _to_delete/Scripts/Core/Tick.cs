using System;

// Dungeon Tools 7) 아주 단순한 전역 틱 카운터. 지금은 플레이어가 한 칸 움직일 때마다 1씩
// 증가하는 용도로만 쓴다. 나중에 몬스터 AI/함정 타이머 등이 OnTick을 구독하면 된다.
public static class Tick
{
    public static int Current { get; private set; }
    public static event Action<int> OnTick;

    public static void Advance(int amount = 1)
    {
        Current += amount;
        OnTick?.Invoke(Current);
    }

    public static void ResetForNewRun()
    {
        Current = 0;
    }
}
