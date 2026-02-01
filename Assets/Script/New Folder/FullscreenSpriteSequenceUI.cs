using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FullscreenSpriteSequenceUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public GameObject panelRoot;     // 整个全屏Panel（通常就是自己）
    public Image image;             // 全屏Image
    public bool closeOnClick = true;

    [Header("Frames")]
    public Sprite[] frames;
    public float fps = 12f;
    public bool loop = false;
    public bool holdLastFrame = true; // 播完停最后一帧（loop=false时）

    [Header("Open Behavior")]
    public bool playOnOpen = true;
    public bool resetToFirstFrameOnOpen = true;

    [Header("Block World While Open (optional but recommended)")]
    public Collider2D[] disableColliders;   // 打开时临时禁用（比如 camera 的 colliders）
    public Behaviour[] disableBehaviours;   // 打开时临时禁用（比如 InteractableAction2D/点击脚本）

    [Header("SFX (optional)")]
    public AudioSource sfxSource;
    public AudioClip openClip;
    public AudioClip closeClip;
    [Range(0f, 1f)] public float volume = 1f;

    private bool isOpen;
    private bool isPlaying;
    private int index;
    private float timer;

    void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (image == null) image = GetComponentInChildren<Image>(true);

        // 开局隐藏（你也可以手动在Inspector关）
        panelRoot.SetActive(false);
        isOpen = false;
    }

    void Update()
    {
        if (!isOpen || !isPlaying) return;
        if (frames == null || frames.Length == 0) return;
        if (image == null) return;

        float frameDur = 1f / Mathf.Max(1f, fps);
        timer += Time.unscaledDeltaTime; // 不受TimeScale影响

        while (timer >= frameDur)
        {
            timer -= frameDur;
            AdvanceFrame();
        }
    }

    private void AdvanceFrame()
    {
        if (frames == null || frames.Length == 0) return;

        index++;

        if (index >= frames.Length)
        {
            if (loop)
            {
                index = 0;
            }
            else
            {
                // stop
                isPlaying = false;
                index = holdLastFrame ? frames.Length - 1 : 0;
            }
        }

        image.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }

    // ✅ 给 UnityEvent 绑定：onSuccess -> Open()
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        panelRoot.SetActive(true);
        SetWorldEnabled(false);

        if (sfxSource != null && openClip != null)
            sfxSource.PlayOneShot(openClip, volume);

        if (frames != null && frames.Length > 0 && image != null)
        {
            if (resetToFirstFrameOnOpen)
            {
                index = 0;
                timer = 0f;
                image.sprite = frames[0];
            }
            else
            {
                // 保留上次停留帧
                index = Mathf.Clamp(index, 0, frames.Length - 1);
                image.sprite = frames[index];
            }
        }

        isPlaying = playOnOpen;
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        isPlaying = false;

        if (sfxSource != null && closeClip != null)
            sfxSource.PlayOneShot(closeClip, volume);

        panelRoot.SetActive(false);
        SetWorldEnabled(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!closeOnClick) return;
        Close();
    }

    private void SetWorldEnabled(bool enabled)
    {
        if (disableColliders != null)
        {
            for (int i = 0; i < disableColliders.Length; i++)
                if (disableColliders[i] != null) disableColliders[i].enabled = enabled;
        }

        if (disableBehaviours != null)
        {
            for (int i = 0; i < disableBehaviours.Length; i++)
                if (disableBehaviours[i] != null) disableBehaviours[i].enabled = enabled;
        }
    }
}