using TMPro;
using UnityEngine;

/// <summary>
/// Animate substrings wrapped in TMP <link="id"> ... </link> tags.
/// Supported ids: shake, wave, bounce, rotate, scale, sync_scale.
/// </summary>
public class TMP_LinkAnimator : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;

    [Header("Shake (ID: shake)")]
    public float shakeAmount = 5f;
    public float shakeSpeed = 10f;

    [Header("Wave (ID: wave)")]
    public float waveHeight = 5f;
    public float waveSpeed = 5f;

    [Header("Bounce (ID: bounce)")]
    public float bounceHeight = 10f;
    public float bounceSpeed = 4f;

    [Header("Rotate (ID: rotate)")]
    public float rotateAmount = 10f;
    public float rotateSpeed = 4f;

    [Header("Scale (ID: scale)")]
    public float scaleAmount = 1.2f;
    public float scaleSpeed = 3f;

    [Header("Sync Scale (ID: sync_scale)")]
    public float syncScaleAmount = 1.2f;
    public float syncScaleSpeed = 3f;

    public void BindText(TMP_Text targetText)
    {
        textComponent = targetText;
    }

    private void Awake()
    {
        EnsureTextComponent();
    }

    private void OnValidate()
    {
        EnsureTextComponent();
    }

    private void Update()
    {
        EnsureTextComponent();
        if (textComponent == null || !textComponent.enabled) return;

        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;
        if (textInfo.linkCount == 0 || textInfo.characterCount == 0) return;

        for (int i = 0; i < textInfo.linkCount; i++)
        {
            TMP_LinkInfo link = textInfo.linkInfo[i];
            string linkId = link.GetLinkID();

            if (linkId == "shake")
            {
                ApplyShake(textInfo, link);
            }
            else if (linkId == "wave")
            {
                ApplyWave(textInfo, link);
            }
            else if (linkId == "bounce")
            {
                ApplyBounce(textInfo, link);
            }
            else if (linkId == "rotate")
            {
                ApplyRotation(textInfo, link);
            }
            else if (linkId == "scale")
            {
                ApplyScale(textInfo, link, true);
            }
            else if (linkId == "sync_scale")
            {
                ApplyScale(textInfo, link, false);
            }
        }

        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private void EnsureTextComponent()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }
    }

    private bool ShouldAnimateCharacter(int characterIndex, TMP_TextInfo textInfo)
    {
        if (textComponent == null) return false;
        if (characterIndex < 0 || characterIndex >= textInfo.characterCount) return false;

        int maxVisible = textComponent.maxVisibleCharacters;
        if (maxVisible < int.MaxValue && characterIndex >= maxVisible) return false;

        return textInfo.characterInfo[characterIndex].isVisible;
    }

    private void ApplyShake(TMP_TextInfo textInfo, TMP_LinkInfo link)
    {
        for (int i = link.linkTextfirstCharacterIndex; i < link.linkTextfirstCharacterIndex + link.linkTextLength; i++)
        {
            if (!ShouldAnimateCharacter(i, textInfo)) continue;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 jitterOffset = new Vector3(
                Mathf.Sin(Time.time * shakeSpeed + i * 144.3f) * shakeAmount,
                Mathf.Cos(Time.time * shakeSpeed * 0.8f + i * 534.1f) * shakeAmount,
                0f);

            vertices[vertexIndex + 0] += jitterOffset;
            vertices[vertexIndex + 1] += jitterOffset;
            vertices[vertexIndex + 2] += jitterOffset;
            vertices[vertexIndex + 3] += jitterOffset;
        }
    }

    private void ApplyWave(TMP_TextInfo textInfo, TMP_LinkInfo link)
    {
        for (int i = link.linkTextfirstCharacterIndex; i < link.linkTextfirstCharacterIndex + link.linkTextLength; i++)
        {
            if (!ShouldAnimateCharacter(i, textInfo)) continue;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            float waveOffset = Mathf.Sin(Time.time * waveSpeed + i) * waveHeight;
            Vector3 moveVec = new Vector3(0f, waveOffset, 0f);

            vertices[vertexIndex + 0] += moveVec;
            vertices[vertexIndex + 1] += moveVec;
            vertices[vertexIndex + 2] += moveVec;
            vertices[vertexIndex + 3] += moveVec;
        }
    }

    private void ApplyBounce(TMP_TextInfo textInfo, TMP_LinkInfo link)
    {
        for (int i = link.linkTextfirstCharacterIndex; i < link.linkTextfirstCharacterIndex + link.linkTextLength; i++)
        {
            if (!ShouldAnimateCharacter(i, textInfo)) continue;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            float bounceOffset = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed + i * 0.5f)) * bounceHeight;
            Vector3 moveVec = new Vector3(0f, bounceOffset, 0f);

            vertices[vertexIndex + 0] += moveVec;
            vertices[vertexIndex + 1] += moveVec;
            vertices[vertexIndex + 2] += moveVec;
            vertices[vertexIndex + 3] += moveVec;
        }
    }

    private void ApplyRotation(TMP_TextInfo textInfo, TMP_LinkInfo link)
    {
        for (int i = link.linkTextfirstCharacterIndex; i < link.linkTextfirstCharacterIndex + link.linkTextLength; i++)
        {
            if (!ShouldAnimateCharacter(i, textInfo)) continue;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2f;
            float angle = Mathf.Sin(Time.time * rotateSpeed + i * 0.5f) * rotateAmount;
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, angle), Vector3.one);

            ApplyMatrixToChar(vertices, vertexIndex, matrix, center);
        }
    }

    private void ApplyScale(TMP_TextInfo textInfo, TMP_LinkInfo link, bool isWave)
    {
        float timeValue = Time.time * (isWave ? scaleSpeed : syncScaleSpeed);
        float amount = isWave ? scaleAmount : syncScaleAmount;

        for (int i = link.linkTextfirstCharacterIndex; i < link.linkTextfirstCharacterIndex + link.linkTextLength; i++)
        {
            if (!ShouldAnimateCharacter(i, textInfo)) continue;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2f;
            float phase = isWave ? i : 0f;
            float scale = 1f + Mathf.Sin(timeValue + phase) * (amount - 1f);
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

            ApplyMatrixToChar(vertices, vertexIndex, matrix, center);
        }
    }

    private static void ApplyMatrixToChar(Vector3[] vertices, int vertexIndex, Matrix4x4 matrix, Vector3 center)
    {
        vertices[vertexIndex + 0] = center + matrix.MultiplyPoint3x4(vertices[vertexIndex + 0] - center);
        vertices[vertexIndex + 1] = center + matrix.MultiplyPoint3x4(vertices[vertexIndex + 1] - center);
        vertices[vertexIndex + 2] = center + matrix.MultiplyPoint3x4(vertices[vertexIndex + 2] - center);
        vertices[vertexIndex + 3] = center + matrix.MultiplyPoint3x4(vertices[vertexIndex + 3] - center);
    }
}
