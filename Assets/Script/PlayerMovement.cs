using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    public enum Facing { Vertical, Left, Right } // Vertical = up/down 共用一套

    [Header("Move (Top-Down 2D)")]
    public float moveSpeed = 4.5f;
    public bool normalizeDiagonal = true;

    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;

    [Header("Idle Sprites")]
    public Sprite idleVertical;   // 上/下静止共用
    public Sprite idleLeft;       // 左静止（右静止用镜像）。可选；不填则用 idleVertical

    [Header("Walk Frames")]
    public Sprite[] walkVertical; // 上/下走路共用
    public Sprite[] walkLeft;     // 左走路（右走路用镜像）

    [Header("Walk Animation")]
    public float frameRate = 12f;

    [Header("Move Sound")]
    public AudioClip moveLoopClip;
    [Range(0f, 1f)] public float moveVolume = 0.7f;

    private Rigidbody2D rb;
    private AudioSource audioSource;

    private Vector2 input;
    private bool isMoving;

    private Facing facing = Facing.Vertical;
    private Facing lastFacing = Facing.Vertical;

    private Sprite[] currentFrames;
    private int frameIndex;
    private float frameTimer;

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

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyIdle(lastFacing);
    }

    void Update()
    {
        ReadKeyboardInput(); // ✅ Input System

        isMoving = input.sqrMagnitude > 0.0001f;

        if (isMoving)
        {
            facing = GetFacingFromInput(input);
            lastFacing = facing;

            UpdateCurrentFrames(facing);
            ApplyFlipX(facing);
        }

        HandleMoveSound();
        HandleSprites();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }

    private void ReadKeyboardInput()
    {
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

        if (isMoving)
        {
            if (!audioSource.isPlaying) audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying) audioSource.Stop();
        }
    }

    private void HandleSprites()
    {
        if (spriteRenderer == null) return;

        if (!isMoving)
        {
            ApplyIdle(lastFacing);
            ApplyFlipX(lastFacing);
            frameTimer = 0f;
            frameIndex = 0;
            return;
        }

        if (currentFrames == null || currentFrames.Length == 0)
        {
            ApplyIdle(lastFacing);
            return;
        }

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, frameRate);

        if (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % currentFrames.Length;
            spriteRenderer.sprite = currentFrames[frameIndex];
        }
    }

    private void ApplyIdle(Facing f)
    {
        Sprite s;

        if (f == Facing.Vertical)
            s = idleVertical;
        else
            s = (idleLeft != null) ? idleLeft : idleVertical;

        if (s != null) spriteRenderer.sprite = s;
    }

    private void UpdateCurrentFrames(Facing f)
    {
        Sprite[] newFrames = (f == Facing.Vertical) ? walkVertical : walkLeft;

        if (newFrames != currentFrames)
        {
            currentFrames = newFrames;
            frameIndex = 0;
            frameTimer = 0f;

            if (currentFrames != null && currentFrames.Length > 0)
                spriteRenderer.sprite = currentFrames[0];
        }
    }

    private void ApplyFlipX(Facing f)
    {
        if (spriteRenderer == null) return;

        // 默认：walkLeft / idleLeft 原图是“朝左”
        // 右：镜像
        if (f == Facing.Right) spriteRenderer.flipX = true;
        else spriteRenderer.flipX = false;
    }

    private Facing GetFacingFromInput(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x >= 0 ? Facing.Right : Facing.Left;
        else
            return Facing.Vertical; // up/down 同一套
    }
}