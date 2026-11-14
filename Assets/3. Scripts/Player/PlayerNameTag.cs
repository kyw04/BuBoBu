using Fusion;
using TMPro;
using UnityEngine;

public class PlayerNameTag : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
   
    private Entity entity;
    private NetworkString<_16> shownName;
    
    public override void Spawned()
    {
        entity = GetComponentInParent<Entity>();
    }
    
    public override void Render()
    {
        if (nameText == null || entity == null) return;

        if (entity.PlayerName != shownName)
        {
            shownName = entity.PlayerName;
            nameText.text = shownName.ToString();
        }
    }
}