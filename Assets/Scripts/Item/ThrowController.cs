using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AimController), typeof(InventoryManager))]
public class ThrowController : MonoBehaviour
{
    [Header("Charge Settings")]
    public float minSize = 1f;
    public float maxSize = 5f;
    public float chargeSpeed = 4f;

    [Header("Portal Settings")]
    public float fixedPortalSize = 3f;

    [Header("Projectile Settings")]
    public ItemProjectile projectilePrefab;
    public Transform throwPoint;

    [Header("Recall Settings")]
    public Key recallKey = Key.E;

    private const float PreviewScaleMultiplier = 2f;

    private AimController aimController;
    private InventoryManager inventory;
    private PlayerInput controls;
    private readonly List<GameObject> activeThrownPartialZones = new List<GameObject>();

    private bool isCharging;
    private float currentChargeSize;

    private void Awake()
    {
        aimController = GetComponent<AimController>();
        inventory = GetComponent<InventoryManager>();
        controls = new PlayerInput();

        controls.Player.UseItem.started += OnUseItemStarted;
        controls.Player.UseItem.canceled += OnUseItemReleased;
        controls.Player.Cancel.started += OnCancelPressed;
    }

    private void OnEnable()
    {
        controls.Enable();
        EventManager.Instance.AddListener(EventName.OnCurrentItemChanged, OnItemChangedInterrupt);
    }

    private void OnDisable()
    {
        controls.Disable();
        EventManager.Instance.RemoveListener(EventName.OnCurrentItemChanged, OnItemChangedInterrupt);
    }

    private void Update()
    {
        if (GameplayInputBlocker.IsBlocked)
        {
            if (isCharging) InterruptCharge();
            return;
        }

        if (isCharging && inventory.CurrentItem == ItemType.PartialZone)
        {
            currentChargeSize += chargeSpeed * Time.deltaTime;
            currentChargeSize = Mathf.Clamp(currentChargeSize, minSize, maxSize);

            if (aimController.previewCircle != null)
            {
                float scaleValue = currentChargeSize * PreviewScaleMultiplier;
                aimController.previewCircle.localScale = new Vector3(scaleValue, scaleValue, 1f);
            }
        }

        if (Keyboard.current != null && Keyboard.current[recallKey].wasPressedThisFrame)
        {
            RecallAllPartialZones();
        }
    }

    private void OnUseItemStarted(InputAction.CallbackContext ctx)
    {
        if (GameplayInputBlocker.IsBlocked) return;
        if (inventory.CurrentItem == ItemType.None) return;

        if (inventory.CurrentItem == ItemType.PartialZone)
        {
            isCharging = true;
            currentChargeSize = minSize;

            if (aimController.previewCircle != null)
            {
                aimController.previewCircle.gameObject.SetActive(true);
                float initialScale = currentChargeSize * PreviewScaleMultiplier;
                aimController.previewCircle.localScale = new Vector3(initialScale, initialScale, 1f);
            }
        }
        else if (inventory.CurrentItem == ItemType.Portal)
        {
            ExecuteThrow(ItemType.Portal, fixedPortalSize);
        }
    }

    private void OnUseItemReleased(InputAction.CallbackContext ctx)
    {
        if (GameplayInputBlocker.IsBlocked) return;

        if (isCharging && inventory.CurrentItem == ItemType.PartialZone)
        {
            ExecuteThrow(ItemType.PartialZone, currentChargeSize);
        }
    }

    private void OnCancelPressed(InputAction.CallbackContext ctx)
    {
        if (GameplayInputBlocker.IsBlocked) return;

        if (isCharging)
        {
            InterruptCharge();
            Debug.Log("Charge canceled.");
        }
        else if (inventory.CurrentItem != ItemType.None)
        {
            inventory.SetCurrentItem(ItemType.None);
        }
    }

    private void OnItemChangedInterrupt(object sender, EventArgs e)
    {
        if (isCharging) InterruptCharge();
    }

    private void InterruptCharge()
    {
        isCharging = false;
        currentChargeSize = minSize;

        if (aimController.previewCircle != null)
        {
            aimController.previewCircle.gameObject.SetActive(false);
        }
    }

    private void ExecuteThrow(ItemType itemType, float finalSize)
    {
        if (inventory.GetItemCount(itemType) <= 0) return;

        inventory.ModifyItemCount(itemType, -1);
        Vector2 targetPos = aimController.CurrentTargetPos;

        if (projectilePrefab != null)
        {
            Vector2 startPos = throwPoint != null ? (Vector2)throwPoint.position : (Vector2)transform.position;
            ItemProjectile proj = Instantiate(projectilePrefab, startPos, Quaternion.identity);
            proj.Fire(itemType, targetPos, finalSize, this);
        }
        else
        {
            Debug.LogError("Projectile prefab is not assigned.");
        }

        InterruptCharge();
    }

    public void RegisterThrownPartialZone(GameObject zoneObject)
    {
        if (zoneObject != null)
        {
            activeThrownPartialZones.Add(zoneObject);
        }
    }

    private void RecallAllPartialZones()
    {
        activeThrownPartialZones.RemoveAll(zone => zone == null);
        int reclaimedCount = activeThrownPartialZones.Count;
        if (reclaimedCount <= 0) return;

        foreach (GameObject zone in activeThrownPartialZones)
        {
            Destroy(zone);
        }

        activeThrownPartialZones.Clear();
        inventory.ModifyItemCount(ItemType.PartialZone, reclaimedCount);
        Debug.Log($"Recalled {reclaimedCount} partial switch items.");
    }
}
