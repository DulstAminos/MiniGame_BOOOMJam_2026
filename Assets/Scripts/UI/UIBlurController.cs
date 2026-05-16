using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIBlurController : MonoBehaviour
{
    private Image backgroundImage;
    private Material blurMaterialInstance;
    private Coroutine blurCoroutine;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        InitializeBlur();
    }

    private void InitializeBlur()
    {
        Shader blurShader = Resources.Load<Shader>("Shaders/UIGaussianBlurSprite");
        if (blurShader == null)
        {
            Debug.LogWarning("未找到模糊 Shader: Resources/Shaders/UIGaussianBlurSprite");
            return;
        }

        blurMaterialInstance = new Material(blurShader)
        {
            name = "MainMenuBackgroundBlur (Runtime)"
        };
        backgroundImage.material = blurMaterialInstance;
        SetBlur(0f);
    }

    public void AnimateBlur(float from, float to, float duration, AnimationCurve curve)
    {
        if (blurCoroutine != null) StopCoroutine(blurCoroutine);
        blurCoroutine = StartCoroutine(DoAnimateBlur(from, to, duration, curve));
    }

    private IEnumerator DoAnimateBlur(float from, float to, float duration, AnimationCurve curve)
    {
        if (blurMaterialInstance == null || duration <= 0f)
        {
            SetBlur(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = curve != null ? curve.Evaluate(t) : t;

            SetBlur(Mathf.LerpUnclamped(from, to, curveValue));
            yield return null;
        }

        SetBlur(to);
    }

    public void SetBlur(float blurValue)
    {
        if (blurMaterialInstance != null)
        {
            blurMaterialInstance.SetFloat("_BlurSize", Mathf.Max(0f, blurValue));
        }
    }

    private void OnDestroy()
    {
        if (blurMaterialInstance != null)
        {
            Destroy(blurMaterialInstance);
        }
    }
}
