using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>Spins a visual (thrown bones, crosses, runes, coins) around a local axis.</summary>
    public sealed class VfxSpin : MonoBehaviour
    {
        public Vector3 Axis = Vector3.up;
        public float DegreesPerSecond = 540f;

        private void Update() => transform.Rotate(Axis, DegreesPerSecond * Time.deltaTime, Space.Self);
    }
}
