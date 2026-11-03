using System;

// 현재 InDungeon의 GridMover와 호환되는 기존 정수 Tick 카운터다.
// Dungeon Tools 7) 아주 단순한 전역 틱 카운터. 지금은 플레이어가 한 칸 움직일 때마다 1씩
// 증가하는 용도로만 쓴다. 나중에 몬스터 AI/함정 타이머 등이 OnTick을 구독하면 된다.
public static class Tick
{
    public static int Current { get; private set; }
    public static event Action<int> OnTick;

    public static void Advance(int amount = 1, Func<bool> continueCondition = null)
    {
        int safeAmount = Math.Max(0, amount);
        for (int i = 0; i < safeAmount; i++)
        {
            if (continueCondition != null && !continueCondition()) break;
            Current++;
            OnTick?.Invoke(Current);
        }
    }

    public static void ResetForNewRun()
    {
        Current = 0;
    }
}
