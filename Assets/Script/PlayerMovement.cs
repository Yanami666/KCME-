using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public enum Facing { Down, Up, Left, Right }

    [Header("Move (Top-Down 2D)")]
    public float moveSpeed = 4.5f;
    public bool normalizeDiagonal = true;

    [Header("Renderer")]
    public SpriteRenderer bodyRenderer;

    [Header("Idle (Single Sprite)")]
    public Sprite idleSprite;
    public float idleDelay = 0.15f;

    [Header("Walk Frames (Directional)")]
    public Sprite[] walkRight; // right is original, left uses flipX
    public Sprite[] walkUp;
    public Sprite[] walkDown;
    public float walkFps = 12f;

    [Header("Audio Sources")]
    public AudioSource moveSource; // loop for footsteps
    public AudioSource sfxSource;  // one-shot SFX

    [Header("Move Sound")]
    public AudioClip moveLoopClip;
    [Range(0f, 1f)] public float moveVolume = 0.7f;

    [Header("Action Sound Clips")]
    public AudioClip peeClip;
    public AudioClip fartClip;
    public AudioClip danceClip;
    [Range(0f, 1f)] public float actionSfxVolume = 1.0f;

    [Header("Pee (J) Visuals")]
    public Sprite[] peeFrames;
    public float peeFps = 10f;
    public float peeLockSeconds = 0.8f;
    public GameObject peePrefab;
    public Transform peeSpawnPoint;                 // ✅ will mirror with facing
    public Vector3 peeFallbackWorldOffset = new Vector3(0f, -0.15f, 0f);

    [Header("Fart (K) Visuals")]
    public SpriteRenderer fartRenderer;            // child renderer, placed for RIGHT
    public Sprite[] fartFrames;
    public float fartFps = 12f;
    public float fartShowSeconds = 0.6f;

    [Header("Dance (L) Visuals")]
    public Sprite[] danceFrames;
    public float danceFps = 12f;
    public bool lockMovementWhileDancing = true;

    // ========= runtime =========
    private Rigidbody2D rb;
    private Vector2 input;
    private bool isMoving;

    private Facing lastFacing = Facing.Down;

    // body anim timers
    private float animTimer;
    private int animIndex;

    // idle delay
    private float idleTimer;

    // pee override
    private bool isPeeing;
    private float peeLockTimer;

    // dance override
    private bool isDancing;

    // fart overlay
    private bool fartPlaying;
    private float fartTimer;
    private float fartFrameTimer;
    private int fartFrameIndex;

    // mirror bases (local positions)
    private Vector3 peeSpawnLocalBase;
    private Vector3 fartLocalBase;

    public bool IsMoving => isMoving;
    public Facing CurrentFacing => lastFacing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        // AudioSource auto setup
        if (moveSource == null)
        {
            moveSource = GetComponent<AudioSource>();
            if (moveSource == null) moveSource = gameObject.AddComponent<AudioSource>();
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        moveSource.playOnAwake = false;
        moveSource.loop = true;
        moveSource.volume = moveVolume;
        moveSource.clip = moveLoopClip;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = actionSfxVolume;

        if (fartRenderer != null)
            fartRenderer.gameObject.SetActive(false);

        // cache base local positions for mirroring (avoid drift)
        if (peeSpawnPoint != null)
            peeSpawnLocalBase = peeSpawnPoint.localPosition;

        if (fartRenderer != null)
            fartLocalBase = fartRenderer.transform.localPosition;

        ApplyFacingFlipAndMirror(lastFacing);

        // initial idle
        if (idleSprite != null && bodyRenderer != null)
            bodyRenderer.sprite = idleSprite;
    }

    void Update()
    {
        // pee timer
        if (isPeeing)
        {
            peeLockTimer -= Time.deltaTime;
            if (peeLockTimer <= 0f)
            {
                isPeeing = false;
                animTimer = 0f;
                animIndex = 0;
            }
        }

        ReadKeyboardMoveInput();

        isMoving = input.sqrMagnitude > 0.0001f;

        // update facing only when we are allowed to move
        if (!isPeeing && !(lockMovementWhileDancing && isDancing))
        {
            if (isMoving)
                lastFacing = GetFacingFromInput(input);
        }

        ApplyFacingFlipAndMirror(lastFacing);
        HandleMoveSound();

        // body animation priority: Pee > Dance > Walk > Idle(after delay)
        if (isPeeing)
        {
            PlayBodyFrames(peeFrames, peeFps);
        }
        else if (isDancing)
        {
            PlayBodyFrames(danceFrames, danceFps);
        }
        else if (isMoving)
        {
            idleTimer = 0f;
            PlayWalkByFacing(lastFacing);
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleDelay)
                ShowIdle();
        }

        UpdateFartOverlay();
    }

    void FixedUpdate()
    {
        if (isPeeing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (lockMovementWhileDancing && isDancing)
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

        if (lockMovementWhileDancing && isDancing)
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

        float x = 0f, y = 0f;
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

        bool shouldPlay = isMoving && !isPeeing && !(lockMovementWhileDancing && isDancing);

        if (shouldPlay)
        {
            if (!moveSource.isPlaying) moveSource.Play();
        }
        else
        {
            if (moveSource.isPlaying) moveSource.Stop();
        }
    }

    private Facing GetFacingFromInput(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x >= 0 ? Facing.Right : Facing.Left;
        else
            return v.y >= 0 ? Facing.Up : Facing.Down;
    }

    /// <summary>
    /// Mirror: body flipX + peeSpawnPoint local mirror + fartRenderer local mirror
    /// </summary>
    private void ApplyFacingFlipAndMirror(Facing f)
    {
        if (bodyRenderer == null) return;

        bool left = (f == Facing.Left);

        // Body: right is original, left mirrored
        bodyRenderer.flipX = left;

        // Pee spawn point mirror (local)
        if (peeSpawnPoint != null)
        {
            Vector3 p = peeSpawnLocalBase;
            p.x = left ? -Mathf.Abs(p.x) : Mathf.Abs(p.x);
            peeSpawnPoint.localPosition = p;
        }

        // Fart VFX mirror (local)
        if (fartRenderer != null)
        {
            Vector3 fp = fartLocalBase;
            fp.x = left ? -Mathf.Abs(fp.x) : Mathf.Abs(fp.x);
            fartRenderer.transform.localPosition = fp;
            fartRenderer.flipX = left;
        }
    }

    private void PlayWalkByFacing(Facing f)
    {
        Sprite[] frames;

        if (f == Facing.Right || f == Facing.Left)
            frames = walkRight; // left uses flipX
        else if (f == Facing.Up)
            frames = walkUp;
        else
            frames = walkDown;

        PlayBodyFrames(frames, walkFps);
    }

    private void ShowIdle()
    {
        if (bodyRenderer == null || idleSprite == null) return;
        bodyRenderer.sprite = idleSprite;
    }

    private void PlayBodyFrames(Sprite[] frames, float fps)
    {
        if (bodyRenderer == null) return;
        if (frames == null || frames.Length == 0) return;

        if (frames.Length == 1 || fps <= 0.01f)
        {
            bodyRenderer.sprite = frames[0];
            return;
        }

        animTimer += Time.deltaTime;
        float dt = 1f / Mathf.Max(1f, fps);

        if (animTimer >= dt)
        {
            animTimer -= dt;
            animIndex = (animIndex + 1) % frames.Length;
            if (frames[animIndex] != null)
                bodyRenderer.sprite = frames[animIndex];
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, actionSfxVolume);
    }

    // =========================================================
    // PUBLIC: call these when your gameplay logic says success
    // =========================================================

    /// <summary>J pee success: anim + lock + spawn pee decal + sound</summary>
    public void PlayPeeFeedback()
    {
        PlayOneShot(peeClip);

        if (peeFrames != null && peeFrames.Length > 0)
        {
            isPeeing = true;
            peeLockTimer = Mathf.Max(0.1f, peeLockSeconds);
            animTimer = 0f;
            animIndex = 0;
        }

        if (peePrefab != null)
        {
            Vector3 pos = (peeSpawnPoint != null)
                ? peeSpawnPoint.position
                : transform.position + peeFallbackWorldOffset;

            Instantiate(peePrefab, pos, Quaternion.identity);
        }
    }

    /// <summary>K fart success: overlay anim + sound</summary>
    public void PlayFartFeedback()
    {
        PlayOneShot(fartClip);

        if (fartRenderer == null) return;
        if (fartFrames == null || fartFrames.Length == 0) return;

        fartPlaying = true;
        fartTimer = fartShowSeconds;
        fartFrameTimer = 0f;
        fartFrameIndex = 0;

        fartRenderer.gameObject.SetActive(true);
        fartRenderer.sprite = fartFrames[0];
    }

    /// <summary>L dance on/off (for PlayerKeyActions call). Name matches your error.</summary>
    public void SetDanceFeedbackActive(bool active)
    {
        if (active)
        {
            if (danceFrames == null || danceFrames.Length == 0) return;

            if (!isDancing)
            {
                isDancing = true;
                animTimer = 0f;
                animIndex = 0;
                PlayOneShot(danceClip);
            }
        }
        else
        {
            if (!isDancing) return;
            isDancing = false;
            animTimer = 0f;
            animIndex = 0;
        }
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