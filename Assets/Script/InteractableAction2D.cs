using UnityEngine;
using UnityEngine.Events;

public class InteractableAction2D : MonoBehaviour
{
    [Header("Core Refs")]
    public ActionExecutor executor;
    public VisibilityProbe2D playerProbe;
    public VisibilityProbe2D targetProbe; // optional (attach VisibilityProbe2D to this object if needed)
    public GoodHintUI goodHintUI;

    [Header("Rule")]
    public ActionExecutor.ActionType actionType = ActionExecutor.ActionType.Good;
    public int customDelta = 0; // 0 = use ActionExecutor defaults

    [Header("Proximity")]
    public bool requireInRange = true;

    [Header("Blink")]
    public SpriteRenderer spriteRenderer;
    public float blinkSpeed = 6f;
    public float blinkMinAlpha = 0.35f;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip successClip;
    [Range(0f, 1f)] public float successVolume = 1f;

    [Header("Messages")]
    public string goodFailMessage = "You need to be in NPC FOV to do this good action.";

    [Header("Events")]
    public UnityEvent onSuccess;
    public UnityEvent onFailNoChange;
    public UnityEvent onArrested;

    [Header("Consume")]
    public bool disableOnSuccess = false; // if true: disables collider + renderer

    private bool inRange;
    private Color baseColor;
    private float blinkT;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;

        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();

        // Try auto-wire common refs if placed near Player/GameManager (safe, no hard fail)
        if (executor == null)
        {
            var gm = FindObjectOfType<ActionExecutor>();
            executor = gm;
        }
    }

    public void SetInRange(bool v)
    {
        inRange = v;

        if (!inRange)
        {
            // restore
            if (spriteRenderer != null)
                spriteRenderer.color = baseColor;
            blinkT = 0f;
        }
    }

    void Update()
    {
        if (!inRange) return;
        if (spriteRenderer == null) return;

        blinkT += Time.deltaTime * Mathf.Max(0.01f, blinkSpeed);
        float a = Mathf.Lerp(blinkMinAlpha, 1f, (Mathf.Sin(blinkT) * 0.5f + 0.5f));

        Color c = baseColor;
        c.a = a;
        spriteRenderer.color = c;
    }

    public void TryInteract()
    {
        if (requireInRange && !inRange) return;
        if (executor == null) return;

        bool arrested;
        string reason;

        bool ok = executor.TryExecute(
            actionType,
            playerProbe,
            targetProbe,
            customDelta,
            showGoodFailHint: (actionType == ActionExecutor.ActionType.Good),
            out reason,
            out arrested
        );

        if (arrested)
        {
            onArrested?.Invoke();
            return;
        }

        if (ok)
        {
            if (sfxSource != null && successClip != null)
                sfxSource.PlayOneShot(successClip, successVolume);

            onSuccess?.Invoke();

            if (disableOnSuccess)
            {
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                if (spriteRenderer != null) spriteRenderer.enabled = false;
                enabled = false;
            }
        }
        else
        {
            onFailNoChange?.Invoke();

            if (actionType == ActionExecutor.ActionType.Good && goodHintUI != null)
                goodHintUI.Show(goodFailMessage);
        }
    }
}