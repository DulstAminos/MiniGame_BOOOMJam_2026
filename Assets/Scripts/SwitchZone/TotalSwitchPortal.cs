using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 完全切换道具生成的传送门区域
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TotalSwitchPortal : MonoBehaviour
{
    [Header("Return Settings")]
    [SerializeField] private float returnDelaySeconds = 3f;

    [Header("Flash Settings")]
    [SerializeField] private float flashStepDuration = 0.08f;
    [SerializeField, Range(0f, 1f)] private float darkFlashAlpha = 0.18f;
    [SerializeField, Range(0f, 1f)] private float brightFlashAlpha = 0.14f;

    private Collider2D triggerCollider;
    private ThrowController ownerThrowController;
    private bool isConsumed;
    private WorldType returnWorld;

    public void Initialize(ThrowController owner)
    {
        ownerThrowController = owner;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isConsumed || collision == null || !collision.CompareTag("Player")) return;

        LevelWorldManager worldManager = LevelWorldManager.Instance;
        if (worldManager == null) return;

        isConsumed = true;
        returnWorld = worldManager.CurrentActiveWorld;
        worldManager.RequestTotalSwitch();
        ConsumePortalAfterUse();
        StartCoroutine(ReturnToOriginalWorldRoutine());
    }

    private void ConsumePortalAfterUse()
    {
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        ZoneController zoneController = GetComponentInParent<ZoneController>();
        if (zoneController != null)
        {
            Destroy(zoneController);
        }

        SpriteMask spriteMask = GetComponent<SpriteMask>();
        if (spriteMask != null)
        {
            spriteMask.enabled = false;
        }
    }

    private IEnumerator ReturnToOriginalWorldRoutine()
    {
        if (returnDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(returnDelaySeconds);
        }

        yield return PlayReturnFlash();

        LevelWorldManager worldManager = LevelWorldManager.Instance;
        if (worldManager != null && worldManager.CurrentActiveWorld != returnWorld)
        {
            worldManager.RequestTotalSwitch();
        }

        ownerThrowController?.RefundItem(ItemType.Portal, 1);

        GameObject portalRoot = GetPortalRootObject();
        Destroy(portalRoot != null ? portalRoot : gameObject);
    }

    private IEnumerator PlayReturnFlash()
    {
        GameObject canvasObject = new GameObject(
            "PortalReturnFlash",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlayObject = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = Color.clear;
        overlayImage.raycastTarget = false;

        Color darkColor = new Color(0f, 0f, 0f, darkFlashAlpha);
        Color brightColor = new Color(1f, 1f, 1f, brightFlashAlpha);

        yield return LerpOverlayColor(overlayImage, Color.clear, darkColor, flashStepDuration);
        yield return LerpOverlayColor(overlayImage, darkColor, brightColor, flashStepDuration);
        yield return LerpOverlayColor(overlayImage, brightColor, Color.clear, flashStepDuration * 1.5f);

        Destroy(canvasObject);
    }

    private IEnumerator LerpOverlayColor(Image overlayImage, Color from, Color to, float duration)
    {
        if (overlayImage == null) yield break;

        if (duration <= 0f)
        {
            overlayImage.color = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            overlayImage.color = Color.LerpUnclamped(from, to, t);
            yield return null;
        }

        overlayImage.color = to;
    }

    private GameObject GetPortalRootObject()
    {
        ZoneController zoneController = GetComponentInParent<ZoneController>();
        if (zoneController != null)
        {
            return zoneController.gameObject;
        }

        return transform.parent != null ? transform.parent.gameObject : gameObject;
    }
}
