using System.Collections.Generic;
using Fusion;
using PangPangShotNetwork;
using UnityEngine;

public enum GamePhase
{
    Lobby,         // 방 대기실: 팀 선택, 방장이 시작할 때까지 대기
    RoundStart,    // 라운드 시작 카운트다운 (행동 불가)
    Playing,
    AugmentSelect, // 진 팀 증강 선택 (행동 불가)
    GameOver       // 잠시 후 로비로 복귀
}

// 씬에 배치되는 NetworkObject. 라운드 흐름은 서버에서만 진행하고 클라이언트는 Networked 값을 읽기만 함
// 서버는 한 프로세스에서 방(러너)을 여러 개 돌리므로 static 싱글톤 대신 러너별로 조회
public class GameManager : NetworkBehaviour
{
    public const int TeamCount = 2;
    public const int WinsToVictory = 3; // 5판 3선승

    private static readonly Dictionary<NetworkRunner, GameManager> instances = new();

    public static GameManager Get(NetworkRunner runner)
        => runner != null && instances.TryGetValue(runner, out GameManager game) ? game : null;

    [SerializeField] private Transform[] team1SpawnPoints;
    [SerializeField] private Transform[] team2SpawnPoints;
    [SerializeField, Min(1)] private int minPlayersPerTeam = 1; // 각 팀 최소 인원이 차야 시작 가능
    [SerializeField] private float roundStartDelay = 3f;
    [SerializeField] private float augmentTimeLimit = 15f; // 시간 초과 시 남은 선택은 랜덤 적용
    [SerializeField] private float gameOverDuration = 5f;
    [SerializeField] private float emptyRoomTimeout = 30f; // 방을 만든 사람이 안 들어오면 방(러너) 종료
    [SerializeField] private float closeDelay = 1f;         // 방 닫힘 알림(RPC)이 클라이언트에 도착할 여유
    [SerializeField] private ScoreboardUI scoreboard;

    [Networked] public GamePhase Phase { get; private set; }
    [Networked] public int Round { get; private set; }
    [Networked] public int LastRoundWinner { get; private set; } // 무승부면 -1
    [Networked] public TickTimer PhaseTimer { get; private set; }
    [Networked] public PlayerRef Host { get; private set; }
    [Networked, Capacity(TeamCount)] private NetworkArray<int> Wins => default;

    private readonly List<Entity> players = new();
    private readonly List<Bullet> bullets = new();
    private float emptyTime;
    private bool closing;
    private TickTimer closeTimer;
    private bool shuttingDown;

    public bool CanAct => Phase is GamePhase.Lobby or GamePhase.Playing;
    public int MinPlayersPerTeam => minPlayersPerTeam;
    public bool CanStart => Phase == GamePhase.Lobby
                            && CountPlayers(0) >= minPlayersPerTeam
                            && CountPlayers(1) >= minPlayersPerTeam;
    public int GetWins(int team) => Wins[team];
    public float RemainingTime => PhaseTimer.RemainingTime(Runner) ?? 0f;
    public string RoomCode => Runner.SessionInfo.IsValid ? Runner.SessionInfo.Name : string.Empty;

    // 이 러너의 플레이어 목록. 공유 리스트를 재사용하므로 순회 중에 다시 호출하지 말 것
    public IReadOnlyList<Entity> Players
    {
        get
        {
            players.Clear();
            Runner.GetAllBehaviours(players);
            return players;
        }
    }

    public override void Spawned()
    {
        instances[Runner] = this;

        if (HasStateAuthority)
            LastRoundWinner = -1;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Get(runner) == this)
            instances.Remove(runner);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (shuttingDown) return;
        if (closing)
        {
            UpdateClosing();
            return;
        }
        if (CheckHostLeft() || CheckEmptyRoom()) return;

        switch (Phase)
        {
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

            case GamePhase.GameOver:
                if (PhaseTimer.Expired(Runner))
                    ReturnToLobby();
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

    // 서버 전용: 로비가 아니면 다음 라운드까지 사망 상태로 참가
    public void OnPlayerSpawned(Entity entity)
    {
        if (Phase != GamePhase.Lobby)
            entity.Die();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SelectTeam(int team, RpcInfo info = default)
    {
        if (Phase != GamePhase.Lobby) return;
        if (team < 0 || team >= TeamCount) return;

        Entity entity = FindPlayer(info.Source);
        if (entity == null || entity.Team == team) return;

        entity.Team = team;
        entity.Revive(GetSpawnPosition(team, CountPlayers(team) - 1)); // 바뀐 팀 진영으로 이동
    }

    // 방장만 시작 가능
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_StartGame(RpcInfo info = default)
    {
        if (info.Source != Host || !CanStart) return;
        StartRound();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowStatus(string message, float seconds)
    {
        if (scoreboard != null) scoreboard.ShowStatus(message, seconds);
    }

    private void StartRound()
    {
        Round++;
        ClearBullets();

        var teamIndex = new int[TeamCount];
        foreach (Entity entity in Players)
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
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, gameOverDuration);
            return;
        }

        int loser = 1 - winner;
        foreach (Entity entity in Players)
        {
            if (entity.Team == loser && entity.TryGetComponent(out AugmentSelector selector))
                selector.OfferAugments();
        }

        Phase = GamePhase.AugmentSelect;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, augmentTimeLimit);
    }

    // 게임 종료 후 같은 방에서 다시 할 수 있도록 승수/증강을 초기화하고 로비로
    private void ReturnToLobby()
    {
        for (int i = 0; i < TeamCount; i++)
            Wins.Set(i, 0);
        Round = 0;
        LastRoundWinner = -1;

        var teamIndex = new int[TeamCount];
        foreach (Entity entity in Players)
        {
            entity.ResetStats();
            entity.Revive(GetSpawnPosition(entity.Team, teamIndex[entity.Team]++));
        }

        Phase = GamePhase.Lobby;
    }

    // 서버 전용: 로비/이전 라운드에서 남은 총알 제거
    private void ClearBullets()
    {
        bullets.Clear();
        Runner.GetAllBehaviours(bullets);
        foreach (Bullet bullet in bullets)
        {
            if (bullet.Object != null && bullet.Object.IsValid)
                Runner.Despawn(bullet.Object);
        }
        bullets.Clear();
    }

    private bool IsAugmentSelectDone()
    {
        bool timeout = PhaseTimer.Expired(Runner);
        bool done = true;

        foreach (Entity entity in Players)
        {
            if (!entity.TryGetComponent(out AugmentSelector selector) || !selector.IsChoosing) continue;

            if (timeout)
                selector.SelectRandom();
            else
                done = false;
        }

        return done || timeout;
    }

    // 처음 들어온 플레이어가 방장. 방장이 나가면 방을 닫고 모두 내보냄
    private bool CheckHostLeft()
    {
        if (Host == PlayerRef.None)
        {
            foreach (PlayerRef player in Runner.ActivePlayers)
            {
                if (Host == PlayerRef.None || player.PlayerId < Host.PlayerId)
                    Host = player;
            }
            return false;
        }

        if (Runner.IsPlayerValid(Host)) return false;

        CloseRoom("The host left the room.");
        return true;
    }

    // 방을 만든 사람이 들어오지 않은 채로 시간이 지나면 종료
    private bool CheckEmptyRoom()
    {
        if (Host != PlayerRef.None)
        {
            emptyTime = 0f;
            return false;
        }

        emptyTime += Runner.DeltaTime;
        if (emptyTime < emptyRoomTimeout) return false;

        Debug.Log($"[Server] room {RoomCode} closed (empty)");
        ShutdownRoom();
        return true;
    }

    // 서버 전용: 클라이언트에 알리고 잠시 뒤 러너 종료 (그 사이 새 입장은 막음)
    private void CloseRoom(string message)
    {
        if (closing) return;

        closing = true;
        Runner.SessionInfo.IsOpen = false;
        closeTimer = TickTimer.CreateFromSeconds(Runner, closeDelay);
        RPC_RoomClosed(message);
        Debug.Log($"[Server] room {RoomCode} closing: {message}");
    }

    private void UpdateClosing()
    {
        bool anyPlayer = false;
        foreach (PlayerRef _ in Runner.ActivePlayers)
        {
            anyPlayer = true;
            break;
        }

        if (!anyPlayer || closeTimer.ExpiredOrNotRunning(Runner))
            ShutdownRoom();
    }

    private void ShutdownRoom()
    {
        if (shuttingDown) return;

        shuttingDown = true;
        Runner.Shutdown();
    }

    // 클라이언트: 방이 닫혔으니 스스로 나가서 메뉴로 (RoomConnection이 메뉴 씬 로드)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RoomClosed(string message)
    {
        if (Runner.IsServer) return;

        RoomConnection.PendingMessage = message;
        Runner.Shutdown();
    }

    // 클라이언트: 방 나가기 버튼
    public void Leave()
    {
        if (Runner.IsServer) return;
        Runner.Shutdown();
    }

    private Entity FindPlayer(PlayerRef player)
    {
        foreach (Entity entity in Players)
        {
            if (entity.Object.InputAuthority == player)
                return entity;
        }
        return null;
    }

    private int CountPlayers(int team, bool aliveOnly = false)
    {
        int count = 0;
        foreach (Entity entity in Players)
        {
            if (entity.Team != team) continue;
            if (aliveOnly && entity.IsDead) continue;
            count++;
        }
        return count;
    }
}
