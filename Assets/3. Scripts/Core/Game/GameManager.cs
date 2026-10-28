using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum GamePhase
{
    Waiting,       // 양 팀 인원 대기
    RoundStart,    // 라운드 시작 카운트다운 (행동 불가)
    Playing,
    AugmentSelect, // 진 팀 증강 선택 (행동 불가)
    GameOver
}

// 씬에 배치되는 NetworkObject. 라운드 흐름은 서버에서만 진행하고 클라이언트는 Networked 값을 읽기만 함
public class GameManager : NetworkBehaviour
{
    public const int TeamCount = 2;
    public const int WinsToVictory = 3; // 5판 3선승

    public static GameManager Instance { get; private set; }

    [SerializeField] private Transform[] team1SpawnPoints;
    [SerializeField] private Transform[] team2SpawnPoints;
    [SerializeField, Min(1)] private int minPlayersPerTeam = 1;
    [SerializeField] private float roundStartDelay = 3f;
    [SerializeField] private float augmentTimeLimit = 15f; // 시간 초과 시 남은 선택은 랜덤 적용
    [SerializeField] private ScoreboardUI scoreboard;

    [Networked] public GamePhase Phase { get; private set; }
    [Networked] public int Round { get; private set; }
    [Networked] public int LastRoundWinner { get; private set; } // 무승부면 -1
    [Networked] public TickTimer PhaseTimer { get; private set; }
    [Networked, Capacity(TeamCount)] private NetworkArray<int> Wins => default;

    public bool CanAct => Phase is GamePhase.Waiting or GamePhase.Playing;
    public int GetWins(int team) => Wins[team];
    public float RemainingTime => PhaseTimer.RemainingTime(Runner) ?? 0f;

    public override void Spawned()
    {
        Instance = this;

        if (HasStateAuthority)
            LastRoundWinner = -1;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (Phase)
        {
            case GamePhase.Waiting:
                if (CountPlayers(0) >= minPlayersPerTeam && CountPlayers(1) >= minPlayersPerTeam)
                    StartRound();
                break;

            case GamePhase.RoundStart:
                if (PhaseTimer.Expired(Runner))
                    Phase = GamePhase.Playing;
                break;

            case GamePhase.Playing:
                CheckRoundEnd();
                break;

            case GamePhase.AugmentSelect:
                if (IsAugmentSelectDone())
                    StartRound();
                break;
        }
    }

    // 서버 전용: 인원이 적은 팀에 배정
    public int GetTeamForNewPlayer() => CountPlayers(0) <= CountPlayers(1) ? 0 : 1;

    public Vector3 GetSpawnPosition(int team, int index)
    {
        Transform[] points = team == 0 ? team1SpawnPoints : team2SpawnPoints;
        if (points == null || points.Length == 0) return transform.position;
        return points[index % points.Length].position;
    }

    // 서버 전용: 대기 중이 아니면 다음 라운드까지 사망 상태로 참가
    public void OnPlayerSpawned(Entity entity)
    {
        if (Phase != GamePhase.Waiting)
            entity.Die();
    }

    private void StartRound()
    {
        Round++;

        var teamIndex = new int[TeamCount];
        foreach (Entity entity in Entity.All)
            entity.Revive(GetSpawnPosition(entity.Team, teamIndex[entity.Team]++));

        Phase = GamePhase.RoundStart;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundStartDelay);
    }

    private void CheckRoundEnd()
    {
        int alive1 = CountPlayers(0, aliveOnly: true);
        int alive2 = CountPlayers(1, aliveOnly: true);

        if (alive1 > 0 && alive2 > 0) return;

        if (alive1 == 0 && alive2 == 0)
        {
            // 동시 전멸은 무승부로 처리하고 라운드 재시작
            LastRoundWinner = -1;
            StartRound();
            return;
        }

        EndRound(alive1 > 0 ? 0 : 1);
    }

    private void EndRound(int winner)
    {
        LastRoundWinner = winner;
        Wins.Set(winner, Wins[winner] + 1);

        if (Wins[winner] >= WinsToVictory)
        {
            Phase = GamePhase.GameOver;
            return;
        }

        int loser = 1 - winner;
        foreach (Entity entity in Entity.All)
        {
            if (entity.Team == loser && entity.TryGetComponent(out AugmentSelector selector))
                selector.OfferAugments();
        }

        Phase = GamePhase.AugmentSelect;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, augmentTimeLimit);
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowStatus(string message, float seconds)
    {
        if (scoreboard != null) scoreboard.ShowStatus(message, seconds);
    }

    private bool IsAugmentSelectDone()
    {
        bool timeout = PhaseTimer.Expired(Runner);
        bool done = true;

        foreach (AugmentSelector selector in GetSelectors())
        {
            if (!selector.IsChoosing) continue;

            if (timeout)
                selector.SelectRandom();
            else
                done = false;
        }

        return done || timeout;
    }

    private IEnumerable<AugmentSelector> GetSelectors()
    {
        foreach (Entity entity in Entity.All)
        {
            if (entity.TryGetComponent(out AugmentSelector selector))
                yield return selector;
        }
    }

    private static int CountPlayers(int team, bool aliveOnly = false)
    {
        int count = 0;
        foreach (Entity entity in Entity.All)
        {
            if (entity.Team != team) continue;
            if (aliveOnly && entity.IsDead) continue;
            count++;
        }
        return count;
    }
}
