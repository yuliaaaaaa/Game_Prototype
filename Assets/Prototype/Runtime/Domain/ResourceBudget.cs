using UnityEngine;

namespace ClearPath
{
    public sealed class ResourceBudget
    {
        public float Initial { get; private set; }
        public float Remaining { get; private set; }
        public float Charged { get; private set; }
        public float Critical { get; private set; }
        public bool Exhausted => Remaining <= Critical + 0.000001f;
        public static float Radius(float volume) => Mathf.Pow(Mathf.Max(0, volume), 1f / 3f);
        public void Reset(float initial, float critical)
        { Initial = Remaining = initial; Critical = critical; Charged = 0; }
        public void Transfer(float amount)
        {
            float transfer = Mathf.Clamp(amount, 0, Mathf.Max(0, Remaining - Critical));
            Remaining -= transfer; Charged += transfer;
        }
        public float Release() { float result = Charged; Charged = 0; return result; }
        public void Cancel() { Remaining += Charged; Charged = 0; }
        public bool CanAfford(float minimum) => Remaining - Critical > minimum;
    }
}
