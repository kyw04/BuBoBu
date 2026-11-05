using Fusion;
using UnityEngine;

// 플레이어 프리팹에 붙음. 서버가 선택지를 Networked로 내려주고, 클라이언트는 RPC로 고른 슬롯만 보냄
[RequireComponent(typeof(PlayerStats))]
public class AugmentSelector : NetworkBehaviour
{
    public const int ChoiceCount = 3;

    [SerializeField] private AugmentDatabase database;
    [SerializeField] private bool offerOnSpawn = true; // 테스트용: 스폰 즉시 증강 선택

    [Networked, Capacity(ChoiceCount)] private NetworkArray<int> Choices => default;
    [Networked] public NetworkBool IsChoosing { get; private set; }
    [Networked] public AugmentRank CurrentRank { get; private set; } // 지금 고르는 선택지의 랭크
    [Networked] public NetworkBool IsGunChoice { get; private set; } // true면 선택지가 증강이 아니라 총 (GunDatabase 인덱스)
    [Networked] public int AugmentCount { get; private set; }        // 이번 게임에서 받은 증강 수 = 다음 랭크 순번

    private PlayerStats playerStats;
    private GunStats gunStats;
    private Gun gun;
    private AugmentUI ui;
    private ChangeDetector changes;
    private readonly int[] pickBuffer = new int[ChoiceCount];

    public AugmentDatabase Database => database;
    public GunDatabase GunDatabase => gun != null ? gun.Database : null;
    public int GetChoice(int slot) => Choices[slot];

    public override void Spawned()
    {
        playerStats = GetComponent<PlayerStats>();
        gunStats = GetComponentInChildren<GunStats>();
        gun = GetComponentInChildren<Gun>();

        if (HasStateAuthority && offerOnSpawn)
            OfferAugments();

        if (HasInputAuthority)
        {
            ui = FindAnyObjectByType<AugmentUI>(FindObjectsInactive.Include);
            changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            RefreshUI();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (ui != null)
            ui.Hide();
    }

    public override void Render()
    {
        if (changes == null) return;

        foreach (var _ in changes.DetectChanges(this))
        {
            RefreshUI();
            break;
        }
    }

    // 서버 전용: 선택지를 뽑아 선택 상태로 전환
    // 일반 증강은 미리 뽑아둔 랭크 순서에서 이번 차례 랭크를 보고 그 랭크 증강 중에서 뽑음
    // gunChoice면 증강 대신 총 중에서 뽑음 (총 목록이 비어 있으면 일반 증강)
    public void OfferAugments(bool gunChoice = false)
    {
        if (!HasStateAuthority) return;

        GunDatabase guns = GunDatabase;
        IsGunChoice = gunChoice && guns != null && guns.Count > 0;

        if (IsGunChoice)
        {
            guns.PickRandom(pickBuffer);
        }
        else
        {
            GameManager game = GameManager.Get(Runner);
            CurrentRank = game != null ? game.GetAugmentRank(AugmentCount) : database.RollRank();
            database.PickRandom(CurrentRank, pickBuffer);
        }

        for (int i = 0; i < ChoiceCount; i++)
            Choices.Set(i, pickBuffer[i]);

        IsChoosing = true;
    }

    // 서버 전용: 게임이 끝나 로비로 돌아갈 때 증강 순번 초기화
    public void ResetAugments()
    {
        if (!HasStateAuthority) return;

        AugmentCount = 0;
        IsChoosing = false;
        IsGunChoice = false;
        if (gun != null)
            gun.ResetGun();
    }

    // 클라이언트(로컬 플레이어)가 UI에서 호출
    public void Select(int slot)
    {
        if (!HasInputAuthority || !IsChoosing) return;
        RPC_Select(slot);
    }

    // 서버 전용: 제한 시간 초과 시 남은 선택지 중 하나를 자동 적용
    public void SelectRandom()
    {
        if (!HasStateAuthority || !IsChoosing) return;

        int valid = 0;
        for (int i = 0; i < ChoiceCount; i++)
            if (Choices[i] >= 0) valid++;

        if (valid == 0)
        {
            IsChoosing = false;
            return;
        }

        ApplyChoice(Random.Range(0, valid));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_Select(int slot) => ApplyChoice(slot);

    private void ApplyChoice(int slot)
    {
        if (!IsChoosing) return;
        if (slot < 0 || slot >= ChoiceCount) return;

        if (Choices[slot] < 0) return;

        if (IsGunChoice)
        {
            gun.SetGun(Choices[slot]);
        }
        else
        {
            AugmentData augment = database.Get(Choices[slot]);
            if (augment == null) return;

            augment.Apply(playerStats, gunStats);
        }

        AugmentCount++;
        IsChoosing = false;
    }

    private void RefreshUI()
    {
        if (ui == null) return;

        if (IsChoosing)
            ui.Show(this);
        else
            ui.Hide();
    }
}
