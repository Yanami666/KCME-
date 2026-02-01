using UnityEngine;
using UnityEngine.Events;

public class InteractableAction2D : MonoBehaviour
{
    [Header("Core Refs")]
    public ActionExecutor executor;
    public VisibilityProbe2D playerProbe;
    public VisibilityProbe2D targetProbe; // optional
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
    public bool disableOnSuccess = false; // if true: disables colliders + renderer + this script

    private bool inRange;
    private Color baseColor;
    private float blinkT;

    // ✅ 新增：成功后彻底禁止再次触发
    private bool consumed;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;

        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();

        if (executor == null)
            executor = FindObjectOfType<ActionExecutor>();
    }

    public void SetInRange(bool v)
    {
        inRange = v;

        if (!inRange)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = baseColor;
            blinkT = 0f;
        }
    }

    void Update()
    {
        if (consumed) return;           // ✅ 彻底阻止
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
        if (consumed) return;           // ✅ 彻底阻止
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
                consumed = true;   // ✅ 标记消耗，任何后续点击都无效
                SetInRange(false);

                // ✅ 关键：禁用“自己 + 子物体”的所有 Collider2D（包括 Trigger）
                var cols = GetComponentsInChildren<Collider2D>(true);
                for (int i = 0; i < cols.Length; i++)
                    if (cols[i] != null) cols[i].enabled = false;

                // 关渲染
                if (spriteRenderer != null) spriteRenderer.enabled = false;

                // 关脚本本体
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