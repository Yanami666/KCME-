using UnityEngine;

public class StatueMimicAction : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMimicSequence playerMimic;   // Player 上的
    public RectTransform picturePanel;        // Canvas里的那张“全屏/大图”Panel

    [Header("Timing")]
    public float mimicTotalSeconds = 3f;      // 玩家模仿总时长
    public float pictureDelay = 1f;           // 1秒后开始出现（进入第2秒）
    public float slideInSeconds = 1f;         // 从下往上滑入的时长（你要1秒）
    public float slideOutSeconds = 0.25f;     // 结束时滑回去（不想滑回去就设0）

    [Header("UI Positions (AnchoredPosition)")]
    public Vector2 shownPos = Vector2.zero;   // 显示时位置（建议就是你现在摆好的位置）
    public float hiddenOffsetY = -900f;       // 隐藏时往下偏移多少（按你分辨率调整）

    [Header("SFX (optional)")]
    public AudioSource sfxSource;
    public AudioClip mimicStartClip;
    public AudioClip pictureShowClip;
    [Range(0f, 1f)] public float volume = 1f;

    private bool running;
    private Vector2 hiddenPos;

    void Awake()
    {
        // 记录隐藏位置 = 显示位置往下挪
        hiddenPos = shownPos + new Vector2(0f, hiddenOffsetY);

        // 初始隐藏（最稳：直接放到底部并 SetActive false）
        if (picturePanel != null)
        {
            picturePanel.anchoredPosition = hiddenPos;
            picturePanel.gameObject.SetActive(false);
        }
    }

    // 绑定到 InteractableAction2D.onSuccess
    public void Trigger()
    {
        if (running) return;
        if (playerMimic == null) return;
        running = true;

        // 开始模仿
        if (sfxSource != null && mimicStartClip != null)
            sfxSource.PlayOneShot(mimicStartClip, volume);

        playerMimic.PlayMimic(mimicTotalSeconds);

        StopAllCoroutines();
        StartCoroutine(Routine());
    }

    private System.Collections.IEnumerator Routine()
    {
        float total = Mathf.Max(0.1f, mimicTotalSeconds);
        float delay = Mathf.Clamp(pictureDelay, 0f, total);

        // 等到第2秒开始出现（= 1秒后）
        yield return new WaitForSeconds(delay);

        // 显示并滑入
        if (picturePanel != null)
        {
            if (sfxSource != null && pictureShowClip != null)
                sfxSource.PlayOneShot(pictureShowClip, volume);

            picturePanel.gameObject.SetActive(true);
            yield return Slide(picturePanel, hiddenPos, shownPos, Mathf.Max(0.01f, slideInSeconds));
        }

        // 等到3秒结束（剩余时间）
        float remain = Mathf.Max(0f, total - delay);
        yield return new WaitForSeconds(remain);

        // 结束：滑回去（可选）+ 隐藏 + 恢复玩家
        if (picturePanel != null)
        {
            if (slideOutSeconds > 0f)
                yield return Slide(picturePanel, shownPos, hiddenPos, slideOutSeconds);

            picturePanel.gameObject.SetActive(false);
        }

        playerMimic.StopMimic();
        running = false;
    }

    private System.Collections.IEnumerator Slide(RectTransform rt, Vector2 from, Vector2 to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seconds);
            rt.anchoredPosition = Vector2.Lerp(from, to, k);
            yield return null;
        }
        rt.anchoredPosition = to;
    }
}