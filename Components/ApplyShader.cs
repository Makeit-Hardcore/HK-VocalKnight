using UnityEngine;

namespace VocalKnight.Components
{
    public class ApplyShader : MonoBehaviour
    {
        public Material CurrentMaterial;

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src, dest, CurrentMaterial);
        }
    }
}
