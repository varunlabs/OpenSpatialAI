using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class HeadsetUiButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image targetImage;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float dwellSeconds = 0.75f;
    [SerializeField] private float maxDistance = 4f;
    [SerializeField] private float cooldownSeconds = 0.7f;

    private BoxCollider hitBox;
    private float dwellTimer;
    private float cooldownUntil;

    private void Awake()
    {
        ResolveReferences();
        RefreshHitBox();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshHitBox();
        dwellTimer = 0f;
    }

    private void Update()
    {
        ResolveReferences();

        bool focused = IsHeadGazeFocused();
        if (focused)
        {
            dwellTimer += Time.deltaTime;
            if ((dwellTimer >= dwellSeconds || IsSelectPressed()) && Time.time >= cooldownUntil)
            {
                cooldownUntil = Time.time + cooldownSeconds;
                dwellTimer = 0f;
                button?.onClick.Invoke();
            }
        }
        else
        {
            dwellTimer = 0f;
        }

        ApplyVisualState(focused);
    }

    public void Configure(Button sourceButton, TMP_Text sourceLabel)
    {
        button = sourceButton;
        label = sourceLabel;
        targetImage = sourceButton != null ? sourceButton.targetGraphic as Image : targetImage;
        ResolveReferences();
        RefreshHitBox();
    }

    private void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (targetImage == null && button != null)
        {
            targetImage = button.targetGraphic as Image;
        }

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void RefreshHitBox()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        hitBox = GetComponent<BoxCollider>();
        if (hitBox == null)
        {
            hitBox = gameObject.AddComponent<BoxCollider>();
        }

        hitBox.isTrigger = true;
        hitBox.size = new Vector3(Mathf.Max(80f, rect.sizeDelta.x), Mathf.Max(40f, rect.sizeDelta.y), 24f);
        hitBox.center = Vector3.zero;
    }

    private bool IsHeadGazeFocused()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        Ray ray = new Ray(camera.transform.position, camera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            return false;
        }

        return hit.collider != null && hit.collider.GetComponentInParent<HeadsetUiButton>() == this;
    }

    private static bool IsSelectPressed()
    {
        return IsSelectPressed(XRNode.RightHand) || IsSelectPressed(XRNode.LeftHand) || Input.GetMouseButtonDown(0);
    }

    private static bool IsSelectPressed(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
        {
            return false;
        }

        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButton) && triggerButton)
        {
            return true;
        }

        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButton) && primaryButton)
        {
            return true;
        }

        return device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) && triggerValue > 0.75f;
    }

    private void ApplyVisualState(bool focused)
    {
        if (targetImage != null)
        {
            targetImage.color = focused
                ? new Color(0.15f, 0.48f, 0.95f, 0.96f)
                : new Color(0.08f, 0.12f, 0.18f, 0.92f);
        }

        if (label != null)
        {
            label.color = Color.white;
        }
    }
}
