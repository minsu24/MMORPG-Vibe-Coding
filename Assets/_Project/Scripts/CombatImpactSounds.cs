using UnityEngine;

[CreateAssetMenu(menuName = "Eastern Fantasy/Audio/Combat Impact Sounds")]
public sealed class CombatImpactSounds : ScriptableObject
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField, Range(0f, 1f)] private float volume = 0.65f;

    public AudioClip[] Clips => clips;
    public float Volume => volume;
}
