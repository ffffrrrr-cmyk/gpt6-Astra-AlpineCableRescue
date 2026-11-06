using UnityEngine;

namespace Frostline
{
    public sealed class RescueCamera : MonoBehaviour
    {
        public RescueGame Game;
        public int Mode;
        public void Snap() { Place(true); }
        void LateUpdate() { Place(false); }
        void Place(bool instant)
        {
            if (Game == null || Game.Crane == null) return;
            Vector3 position; Vector3 target;
            if (Game.Rules.Phase == MissionPhase.Menu)
            {
                position = new Vector3(26, 18, 32); target = new Vector3(-4, 5, 6);
            }
            else if (Mode == 1)
            {
                target = Game.Crane.Hook.position + Vector3.down * 1.2f;
                position = target + new Vector3(13, 10, 16);
            }
            else if (Mode == 2)
            {
                position = new Vector3(0, 43, 19); target = new Vector3(0, 0, 8);
            }
            else
            {
                position = new Vector3(25, 25, 38); target = new Vector3(0, 5, 8);
            }
            float factor = instant ? 1 : 1 - Mathf.Exp(-Time.unscaledDeltaTime * 4);
            transform.position = Vector3.Lerp(transform.position, position, factor);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target - transform.position), factor);
        }
    }
}
