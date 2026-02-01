using UnityEngine;

public class NPCComplimentZone2D : MonoBehaviour
{
    [Header("Refs")]
    public GameObject bubbleObject;                 // ComplimentBubble
    public SpriteRenderer bubbleRenderer;           // bubble sprite renderer
    public Sprite bubbleNormal;
    public Sprite bubbleHeart;

    [Header("Interactable on bubble")]
    public InteractableAction2D bubbleInteractable; // bubble上的 InteractableAction2D
    public Collider2D bubbleCollider;               // bubble的 collider（成功后关掉）

    [Header("Optional: disable this zone after used")]
    public Collider2D zoneCollider;

    [Header("Player")]
    public string playerTag = "Player";

    private bool inRange;
    private bool used;

    void Awake()
    {
        if (zoneCollider == null) zoneCollider = GetComponent<Collider2D>();
        if (bubbleObject != null) bubbleObject.SetActive(false);

        // 自动找引用（如果你忘了拖也尽量不炸）
        if (bubbleObject != null && bubbleRenderer == null)
            bubbleRenderer = bubbleObject.GetComponentInChildren<SpriteRenderer>();

        if (bubbleObject != null && bubbleInteractable == null)
            bubbleInteractable = bubbleObject.GetComponent<InteractableAction2D>();

        if (bubbleObject != null && bubbleCollider == null)
            bubbleCollider = bubbleObject.GetComponent<Collider2D>();

        // 把 onSuccess 绑定到本脚本（避免你每次手动绑）
        if (bubbleInteractable != null)
        {
            // 注意：这里不清空你已有事件，只是额外加一个
            bubbleInteractable.onSuccess.AddListener(OnComplimentSuccess);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (!other || !other.CompareTag(playerTag)) return;

        inRange = true;

        if (bubbleObject != null)
        {
            bubbleObject.SetActive(true);
            if (bubbleRenderer != null && bubbleNormal != null)
                bubbleRenderer.sprite = bubbleNormal;
        }

        // 让 InteractableAction2D 开始闪烁/可交互
        if (bubbleInteractable != null)
            bubbleInteractable.SetInRange(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (used) return;
        if (!other || !other.CompareTag(playerTag)) return;

        inRange = false;

        if (bubbleInteractable != null)
            bubbleInteractable.SetInRange(false);

        if (bubbleObject != null)
            bubbleObject.SetActive(false);
    }

    // ✅ 成功：切成爱心气泡 + 单次锁死（不可再互动）
    public void OnComplimentSuccess()
    {
        if (used) return;
        used = true;

        // 变成爱心气泡，并保持显示（你想保持/隐藏都行）
        if (bubbleObject != null) bubbleObject.SetActive(true);

        if (bubbleRenderer != null && bubbleHeart != null)
            bubbleRenderer.sprite = bubbleHeart;

        // 不再闪烁/不再可点
        if (bubbleInteractable != null)
        {
            bubbleInteractable.SetInRange(false);
            bubbleInteractable.enabled = false;
        }

        if (bubbleCollider != null)
            bubbleCollider.enabled = false;

        // 这个NPC以后不再触发夸奖
        if (zoneCollider != null)
            zoneCollider.enabled = false;
    }
}