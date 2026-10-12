using UnityEngine;

namespace EasternFantasy.Skill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class SwordswomanSlashAnimationEvents : MonoBehaviour
    {
        private SwordswomanSkillCaster caster;
        private SwordswomanDrawingAttack drawingAttack;
        private void Awake()
        {
            caster = GetComponentInParent<SwordswomanSkillCaster>();
            drawingAttack = GetComponentInParent<SwordswomanDrawingAttack>();
        }
        public void DrawingRelease()
        {
            if (drawingAttack != null) drawingAttack.Release();
        }
        public void DoubleSlashHit(int strike)
        {
            if (caster != null) caster.OnDoubleSlashHit(strike);
        }
    }
}
