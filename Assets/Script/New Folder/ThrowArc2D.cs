using UnityEngine;

public class ThrowArc2D : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer spriteRenderer;
    public Transform landingPoint;      // 落点（空物体）
    public Sprite landedSprite;         // 落地后替换的图（可选）

    [Header("Arc Motion")]
    public float duration = 0.6f;       // 飞行时间
    public float arcHeight = 1.2f;      // 抛物线高度（世界单位）
    public AnimationCurve ease = null;  // 可选：不填就线性

    [Header("After Throw")]
    public bool disableAllCollidersOnStart = true; // 一点击成功就禁用所有Collider，防止刷
    public bool disableInteractableScriptOnStart = true; // 关掉 InteractableAction2D（防止重复Try）
    public bool snapToLandingAtEnd = true;

    [Header("Optional SFX")]
    public AudioSource sfxSource;
    public AudioClip throwClip;
    [Range(0f, 1f)] public float throwVolume = 1f;

    private bool isThrowing;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 给 UnityEvent 绑定：成功后调用这个开始扔。
    /// </summary>
    public void StartThrow()
    {
        if (isThrowing) return;
        if (landingPoint == null) return;

        isThrowing = true;

        if (disableAllCollidersOnStart)
        {
            var cols = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols.Length; i++)
                if (cols[i] != null) cols[i].enabled = false;
        }

        if (disableInteractableScriptOnStart)
        {
            var ia = GetComponent<InteractableAction2D>();
            if (ia != null) ia.enabled = false;
        }

        if (sfxSource != null && throwClip != null)
            sfxSource.PlayOneShot(throwClip, throwVolume);

        StopAllCoroutines();
        StartCoroutine(ThrowRoutine());
    }

    private System.Collections.IEnumerator ThrowRoutine()
    {
        Vector3 start = transform.position;
        Vector3 end = landingPoint.position;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.001f, duration));

            float eased = (ease != null && ease.keys != null && ease.length > 0) ? ease.Evaluate(u) : u;

            // base lerp
            Vector3 pos = Vector3.LerpUnclamped(start, end, eased);

            // arc: sin curve 0->pi
            float arc = Mathf.Sin(u * Mathf.PI) * arcHeight;
            pos.y += arc;

            transform.position = pos;
            yield return null;
        }

        if (snapToLandingAtEnd)
            transform.position = end;

        if (spriteRenderer != null && landedSprite != null)
            spriteRenderer.sprite = landedSprite;

        isThrowing = false;
    }
}