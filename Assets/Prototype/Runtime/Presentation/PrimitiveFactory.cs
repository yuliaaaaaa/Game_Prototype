using UnityEngine;

namespace ClearPath
{
    public static class PrimitiveFactory
    {
        public static GameObject Create(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            return go;
        }
        public static void Remove(GameObject go)
        {
            if (!go) return;
            go.SetActive(false);
            if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
        }
    }
}
