using UnityEngine;

namespace Waterline
{
    [ExecuteAlways, RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public sealed class WorldLabel : MonoBehaviour
    {
        public Material template;
        private Material instance;
        private TextMesh text;

        private void OnEnable()
        {
            Font.textureRebuilt += OnFontRebuilt;
            Refresh();
        }

        public void Refresh()
        {
            text = GetComponent<TextMesh>();
            if (template == null || text.font == null) return;
            if (instance == null) instance = new Material(template) { hideFlags = HideFlags.HideAndDontSave };
            text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
            instance.mainTexture = text.font.material.mainTexture;
            GetComponent<MeshRenderer>().sharedMaterial = instance;
        }

        private void OnFontRebuilt(Font font)
        { if (text != null && text.font == font && instance != null) instance.mainTexture = font.material.mainTexture; }

        private void OnDisable()
        {
            Font.textureRebuilt -= OnFontRebuilt;
            if (instance == null) return;
            if (Application.isPlaying) Destroy(instance); else DestroyImmediate(instance);
            instance = null;
        }
    }
}
