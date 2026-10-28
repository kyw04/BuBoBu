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

    private PlayerStats playerStats;
    private GunStats gunStats;
    private AugmentUI ui;
    private ChangeDetector changes;
    private readonly int[] pickBuffer = new int[ChoiceCount];

    public AugmentDatabase Database => database;
    public int GetChoice(int slot) => Choices[slot];

    public override void Spawned()
    {
        playerStats = GetComponent<PlayerStats>();
        gunStats = GetComponentInChildren<GunStats>();

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

    // 서버 전용: 랜덤 선택지를 뽑아 선택 상태로 전환
    public void OfferAugments()
    {
        if (!HasStateAuthority) return;

        database.PickRandom(pickBuffer);
        for (int i = 0; i < ChoiceCount; i++)
            Choices.Set(i, pickBuffer[i]);

        IsChoosing = true;
    }

    // 클라이언트(로컬 플레이어)가 UI에서 호출
    public void Select(int slot)
    {
        if (!HasInputAuthority || !IsChoosing) return;
        RPC_Select(slot);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_Select(int slot)
    {
        if (!IsChoosing) return;
        if (slot < 0 || slot >= ChoiceCount) return;

        AugmentData augment = database.Get(Choices[slot]);
        if (augment == null) return;

        augment.Apply(playerStats, gunStats);
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
