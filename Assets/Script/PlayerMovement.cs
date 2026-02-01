using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    public enum Facing { Down, Up, Left, Right }

    [Header("Move (Top-Down 2D)")]
    public float moveSpeed = 4.5f;
    public bool normalizeDiagonal = true;

    [Header("Renderer")]
    public SpriteRenderer bodyRenderer;

    [Header("Idle Breath (2 frames each)")]
    public Sprite[] idleRightBreath = new Sprite[2]; // 左用镜像
    public Sprite[] idleUpBreath = new Sprite[2];
    public Sprite[] idleDownBreath = new Sprite[2];
    public float idleBreathFps = 2f; // 呼吸速度（2帧来回切）

    [Header("Walk Frames")]
    public Sprite[] walkRight;   // 你做的“右走”
    public Sprite[] walkUp;      // 上走
    public Sprite[] walkDown;    // 下走
    public float walkFps = 12f;

    [Header("Move Sound")]
    public AudioClip moveLoopClip;
    [Range(0f, 1f)] public float moveVolume = 0.7f;

    [Header("Action Visuals - Pee (J)")]
    public Sprite[] peeFrames;
    public float peeFps = 10f;
    public float peeLockSeconds = 0.8f; // 尿尿时锁定移动时间（可调）
    public GameObject peePrefab;        // 生成在地上的“尿”预制体
    public Transform peeSpawnPoint;     // 不填则用玩家脚底位置

    [Header("Action Visuals - Dance (L)")]
    public Sprite[] danceFrames;
    public float danceFps = 12f;

    [Header("Action Visuals - Fart (K)")]
    public SpriteRenderer fartRenderer; // 建议做成玩家child，默认关闭
    public Sprite[] fartFrames;
    public float fartFps = 12f;
    public float fartShowSeconds = 0.6f;
    public Vector2 fartOffsetRight = new Vector2(0.25f, -0.15f);
    public Vector2 fartOffsetLeft = new Vector2(-0.25f, -0.15f);

    // runtime
    private Rigidbody2D rb;
    private AudioSource audioSource;

    private Vector2 input;
    private bool isMoving;

    private Facing facing = Facing.Down;
    private Facing lastFacing = Facing.Down;

    // animation timers
    private float animTimer;
    private int animIndex;

    // action override state
    private bool isPeeing;
    private float peeLockTimer;

    private bool isDancing;

    // fart state (overlay)
    private bool fartPlaying;
    private float fartTimer;
    private float fartFrameTimer;
    private int fartFrameIndex;

    public bool IsMoving => isMoving;
    public Facing CurrentFacing => lastFacing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = moveVolume;
        audioSource.clip = moveLoopClip;

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (fartRenderer != null)
            fartRenderer.gameObject.SetActive(false);

        ApplyFacingFlip(lastFacing);
        SetBodySprite(GetIdleBreathFrames(lastFacing), 0);
    }

    void Update()
    {
        // 1) pee lock (if peeing, movement is locked for a short time)
        if (isPeeing)
        {
            peeLockTimer -= Time.deltaTime;
            if (peeLockTimer <= 0f)
            {
                isPeeing = false;
                // 回到正常动画（idle/walk）
                animTimer = 0f;
                animIndex = 0;
            }
        }

        // 2) read movement input (if peeing, ignore input)
        ReadKeyboardMoveInput();

        isMoving = input.sqrMagnitude > 0.0001f;

        if (!isPeeing && !isDancing)
        {
            if (isMoving)
            {
                facing = GetFacingFromInput(input);
                lastFacing = facing;
            }
        }

        ApplyFacingFlip(lastFacing);

        // 3) movement sound
        HandleMoveSound();

        // 4) body animation (priority: Pee > Dance > Walk/Idle)
        if (isPeeing)
            PlayBodyFrames(peeFrames, peeFps);
        else if (isDancing)
            PlayBodyFrames(danceFrames, danceFps);
        else if (isMoving)
            PlayWalkByFacing(lastFacing);
        else
            PlayIdleBreathByFacing(lastFacing);

        // 5) fart overlay animation
        UpdateFartOverlay();
    }

    void FixedUpdate()
    {
        if (isPeeing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = input * moveSpeed;
    }

    private void ReadKeyboardMoveInput()
    {
        if (isPeeing)
        {
            input = Vector2.zero;
            return;
        }

        var k = Keyboard.current;
        if (k == null)
        {
            input = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;

        if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;

        input = new Vector2(x, y);

        if (normalizeDiagonal && input.sqrMagnitude > 1f)
            input = input.normalized;
    }

    private void HandleMoveSound()
    {
        if (moveLoopClip == null) return;

        // 尿尿/跳舞的时候不播放走路声
        bool shouldPlay = isMoving && !isPeeing && !isDancing;

        if (shouldPlay)
        {
            if (!audioSource.isPlaying) audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying) audioSource.Stop();
        }
    }

    private Facing GetFacingFromInput(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x >= 0 ? Facing.Right : Facing.Left;
        else
            return v.y >= 0 ? Facing.Up : Facing.Down;
    }

    private void ApplyFacingFlip(Facing f)
    {
        if (bodyRenderer == null) return;

        // 你现在“右走”是原版，左走要镜像
        if (f == Facing.Left) bodyRenderer.flipX = true;
        else bodyRenderer.flipX = false;

        // fart 的位置也要跟随镜像（我们在 PlayFartVisual() 里处理位置）
    }

    private void PlayWalkByFacing(Facing f)
    {
        Sprite[] frames;
        float fps = walkFps;

        if (f == Facing.Right || f == Facing.Left)
            frames = walkRight; // 左用 flipX 镜像
        else if (f == Facing.Up)
            frames = walkUp;
        else
            frames = walkDown;

        PlayBodyFrames(frames, fps);
    }

    private void PlayIdleBreathByFacing(Facing f)
    {
        Sprite[] frames = GetIdleBreathFrames(f);
        PlayBodyFrames(frames, idleBreathFps);
    }

    private Sprite[] GetIdleBreathFrames(Facing f)
    {
        if (f == Facing.Right || f == Facing.Left)
            return idleRightBreath; // 左用 flipX
        if (f == Facing.Up)
            return idleUpBreath;
        return idleDownBreath;
    }

    private void PlayBodyFrames(Sprite[] frames, float fps)
    {
        if (bodyRenderer == null) return;
        if (frames == null || frames.Length == 0) return;

        // 只有1张就直接显示
        if (frames.Length == 1 || fps <= 0.01f)
        {
            SetBodySprite(frames, 0);
            return;
        }

        animTimer += Time.deltaTime;
        float dt = 1f / Mathf.Max(1f, fps);

        if (animTimer >= dt)
        {
            animTimer -= dt;
            animIndex = (animIndex + 1) % frames.Length;
            SetBodySprite(frames, animIndex);
        }
    }

    private void SetBodySprite(Sprite[] frames, int index)
    {
        if (bodyRenderer == null) return;
        if (frames == null || frames.Length == 0) return;

        index = Mathf.Clamp(index, 0, frames.Length - 1);
        if (frames[index] != null)
            bodyRenderer.sprite = frames[index];
    }

    // =========================
    // Public visual triggers (called by your gameplay/action scripts)
    // =========================

    /// <summary>Call when J pee action succeeds (and you want visuals + pee decal).</summary>
    public void PlayPeeVisual()
    {
        if (peeFrames == null || peeFrames.Length == 0) return;

        isPeeing = true;
        peeLockTimer = Mathf.Max(0.1f, peeLockSeconds);

        // reset anim to start
        animTimer = 0f;
        animIndex = 0;

        // spawn pee decal on floor (every time, leave it there)
        if (peePrefab != null)
        {
            Vector3 pos;
            if (peeSpawnPoint != null) pos = peeSpawnPoint.position;
            else pos = transform.position + new Vector3(0f, -0.15f, 0f);

            Instantiate(peePrefab, pos, Quaternion.identity);
        }
    }

    /// <summary>Call while holding L (true) / releasing L (false).</summary>
    public void SetDanceVisualActive(bool active)
    {
        if (active)
        {
            if (danceFrames == null || danceFrames.Length == 0) return;
            isDancing = true;
            animTimer = 0f;
            animIndex = 0;
        }
        else
        {
            if (!isDancing) return;
            isDancing = false;
            animTimer = 0f;
            animIndex = 0;
        }
    }

    /// <summary>Call when K fart action succeeds. Plays a fart overlay near butt.</summary>
    public void PlayFartVisual()
    {
        if (fartRenderer == null) return;
        if (fartFrames == null || fartFrames.Length == 0) return;

        fartPlaying = true;
        fartTimer = fartShowSeconds;
        fartFrameTimer = 0f;
        fartFrameIndex = 0;

        fartRenderer.gameObject.SetActive(true);
        fartRenderer.sprite = fartFrames[0];

        // position fart relative to facing (right vs left)
        Vector2 offset = (lastFacing == Facing.Left) ? fartOffsetLeft : fartOffsetRight;
        fartRenderer.transform.localPosition = offset;

        // mirror fart if needed (optional): usually keep same, but you can match flipX
        fartRenderer.flipX = (lastFacing == Facing.Left);
    }

    private void UpdateFartOverlay()
    {
        if (!fartPlaying) return;

        fartTimer -= Time.deltaTime;
        if (fartTimer <= 0f)
        {
            fartPlaying = false;
            if (fartRenderer != null) fartRenderer.gameObject.SetActive(false);
            return;
        }

        // animate fart frames
        fartFrameTimer += Time.deltaTime;
        float dt = 1f / Mathf.Max(1f, fartFps);

        if (fartFrameTimer >= dt)
        {
            fartFrameTimer -= dt;
            fartFrameIndex = (fartFrameIndex + 1) % fartFrames.Length;
            fartRenderer.sprite = fartFrames[fartFrameIndex];
        }
    }
}