using EasternFantasy.Player;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SkullMonkIntroTrigger : MonoBehaviour
{
    [SerializeField] private MON_SkullMonk boss;
    private void OnTriggerEnter2D(Collider2D other) => TryStart(other);
    private void OnTriggerStay2D(Collider2D other) => TryStart(other);
    private void TryStart(Collider2D other)
    {
        if (boss == null || !boss.IsMeditating || !other.CompareTag("Player")) return;
        var movement = other.GetComponentInParent<PlayerMovement2D>();
        if (movement != null && movement.isActiveAndEnabled) boss.TryBeginIntroDialogue();
    }
}
