using UnityEngine;

public class StealableNPCItem : MonoBehaviour
{
    [Header("Core Refs")]
    public ActionExecutor executor;
    public VisibilityProbe2D playerProbe;
    public VisibilityProbe2D itemProbe; // optional but recommended
    public ThiefProgress thiefProgress;
    public MoneyWallet wallet;

    [Header("Steal Tuning")]
    public int badnessDelta = +10;   // +1 cell
    public int moneyGain = 20;
    public bool consumeOnSuccess = true;

    [Header("Blink")]
    public SpriteRenderer spriteRenderer;
    public float blinkSpeed = 6f;
    public float blinkMinAlpha = 0.35f;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip stealClip;
    [Range(0f, 1f)] public float stealVolume = 1f;

    [Header("Feedback Popup (optional)")]
    public GameObject stolenPopup;
    public float popupSeconds = 1f;

    private bool inRange;
    private Color baseColor;
    private float blinkT;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;

        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();

        if (executor == null) executor = FindObjectOfType<ActionExecutor>();
        if (thiefProgress == null) thiefProgress = FindObjectOfType<ThiefProgress>();
        if (wallet == null) wallet = FindObjectOfType<MoneyWallet>();

        if (itemProbe == null) itemProbe = GetComponent<VisibilityProbe2D>();
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
        if (!inRange) return;
        if (executor == null) return;

        bool arrested;
        string reason;

        bool ok = executor.TryExecute(
            ActionExecutor.ActionType.BadHidden,
            playerProbe,
            itemProbe,
            badnessDelta,
            showGoodFailHint: false,
            out reason,
            out arrested
        );

        if (arrested) return;

        if (ok)
        {
            // thief mode + count + money
            if (thiefProgress != null)
            {
                thiefProgress.EnterThiefMode();
                thiefProgress.RegisterSteal();
            }

            if (wallet != null)
                wallet.AddMoney(moneyGain);

            if (sfxSource != null && stealClip != null)
                sfxSource.PlayOneShot(stealClip, stealVolume);

            if (stolenPopup != null)
                StartCoroutine(PopupRoutine());

            if (consumeOnSuccess)
            {
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
                if (spriteRenderer != null) spriteRenderer.enabled = false;

                enabled = false;
            }
        }
    }

    private System.Collections.IEnumerator PopupRoutine()
    {
        stolenPopup.SetActive(true);
        yield return new WaitForSeconds(popupSeconds);
        if (stolenPopup != null) stolenPopup.SetActive(false);
    }
}