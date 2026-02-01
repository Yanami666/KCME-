using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitDoor2D : MonoBehaviour
{
    [Header("Refs")]
    public MoneyWallet wallet;
    public ArrestManager arrestManager;

    [Header("Requirement")]
    public int requiredMoney = 100;

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Win")]
    public GameObject winPanel;
    public float winShowSeconds = 15f;
    public string winSceneName = "StartPage"; // ✅ 场景名（Build Settings里要有）

    [Header("Fail (Slide Up From Bottom)")]
    public RectTransform failPanel;
    public float failSlideSeconds = 0.25f;
    public float failHoldSeconds = 2f;
    public float failHiddenExtra = 600f;

    private bool busy;

    private Vector2 failShownPos;
    private Vector2 failHiddenPos;

    void Awake()
    {
        if (wallet == null) wallet = FindObjectOfType<MoneyWallet>();
        if (arrestManager == null) arrestManager = FindObjectOfType<ArrestManager>();

        if (winPanel != null) winPanel.SetActive(false);

        if (failPanel != null)
        {
            // 你在编辑器里摆好的位置 = 弹出后停住的位置
            failShownPos = failPanel.anchoredPosition;
            failHiddenPos = failShownPos + Vector2.down * failHiddenExtra;

            failPanel.anchoredPosition = failHiddenPos;
            failPanel.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (busy) return;
        if (!other || !other.CompareTag(playerTag)) return;
        if (arrestManager != null && arrestManager.IsArrested) return;

        TryExit();
    }

    private void TryExit()
    {
        if (wallet == null) return;

        if (wallet.Money >= requiredMoney) StartCoroutine(WinRoutine());
        else StartCoroutine(FailRoutine());
    }

    private System.Collections.IEnumerator WinRoutine()
    {
        busy = true;

        if (winPanel != null) winPanel.SetActive(true);

        // 冻结世界
        Time.timeScale = 0f;

        float t = 0f;
        while (t < winShowSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(winSceneName))
            SceneManager.LoadScene(winSceneName);

        busy = false;
    }

    private System.Collections.IEnumerator FailRoutine()
    {
        busy = true;

        if (failPanel == null)
        {
            busy = false;
            yield break;
        }

        // 重新从隐藏位置开始
        failPanel.gameObject.SetActive(true);
        failPanel.anchoredPosition = failHiddenPos;

        // 滑上来
        yield return Slide(failPanel, failHiddenPos, failShownPos, failSlideSeconds);

        // 停2秒（不受TimeScale影响）
        float t = 0f;
        while (t < failHoldSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // 滑下去并隐藏
        yield return Slide(failPanel, failShownPos, failHiddenPos, failSlideSeconds);

        failPanel.gameObject.SetActive(false);

        busy = false;
    }

    private System.Collections.IEnumerator Slide(RectTransform rt, Vector2 from, Vector2 to, float seconds)
    {
        if (rt == null) yield break;

        if (seconds <= 0.001f)
        {
            rt.anchoredPosition = to;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / seconds);
            u = Mathf.SmoothStep(0f, 1f, u);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, u);
            yield return null;
        }

        rt.anchoredPosition = to;
    }
}