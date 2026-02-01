using UnityEngine;

public class PlayerMimicSequence : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMovement playerMovement;      // 直接拖 Player 上的 PlayerMovement
    public Rigidbody2D playerRb;              // 可选：拖 Player 的 Rigidbody2D（不拖也行）
    public SpriteRenderer playerRenderer;     // 拖 Player 的 SpriteRenderer

    [Header("Mimic Frames")]
    public Sprite[] mimicFrames;              // 你模仿雕像的帧图
    public float mimicFrameRate = 10f;        // 每秒几帧

    private bool running;
    private Coroutine routine;
    private Sprite savedSprite;
    private bool savedMovementEnabled;

    void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerRb == null) playerRb = GetComponent<Rigidbody2D>();
        if (playerRenderer == null) playerRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// 开始模仿：持续 totalSeconds，期间禁用 PlayerMovement 防止覆盖 sprite
    /// </summary>
    public void PlayMimic(float totalSeconds)
    {
        if (running) return;
        if (playerRenderer == null) return;

        running = true;

        // 保存当前状态
        savedSprite = playerRenderer.sprite;
        if (playerMovement != null)
        {
            savedMovementEnabled = playerMovement.enabled;
            playerMovement.enabled = false; // ✅ 关键：禁用 movement，避免它下一帧把sprite刷回去
        }

        // 停止移动（可选，但推荐）
        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(MimicRoutine(Mathf.Max(0.1f, totalSeconds)));
    }

    /// <summary>
    /// 强制结束模仿（如果你需要）
    /// </summary>
    public void StopMimic()
    {
        if (!running) return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        Restore();
    }

    private System.Collections.IEnumerator MimicRoutine(float totalSeconds)
    {
        float t = 0f;
        int idx = 0;

        float frameDur = 1f / Mathf.Max(1f, mimicFrameRate);

        // 如果没帧图，就至少保持当前一张
        if (mimicFrames == null || mimicFrames.Length == 0)
        {
            // 维持 savedSprite 不变也行，这里就等时间结束
            while (t < totalSeconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Restore();
            yield break;
        }

        // 播放帧动画
        float frameTimer = 0f;
        playerRenderer.sprite = mimicFrames[0];

        while (t < totalSeconds)
        {
            t += Time.deltaTime;
            frameTimer += Time.deltaTime;

            if (frameTimer >= frameDur)
            {
                frameTimer -= frameDur;
                idx = (idx + 1) % mimicFrames.Length;
                playerRenderer.sprite = mimicFrames[idx];
            }

            yield return null;
        }

        Restore();
    }

    private void Restore()
    {
        // 恢复 sprite
        if (playerRenderer != null)
            playerRenderer.sprite = savedSprite;

        // 恢复 movement
        if (playerMovement != null)
            playerMovement.enabled = savedMovementEnabled;

        running = false;
    }
}