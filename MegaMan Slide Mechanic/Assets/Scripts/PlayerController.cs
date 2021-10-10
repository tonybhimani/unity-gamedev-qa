using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;
    SpriteRenderer sprite;

    float keyHorizontal;
    float keyVertical;
    bool keyJump;
    bool keyShoot;

    bool isGrounded;
    bool isClimbing;
    bool isJumping;
    bool isSliding;
    bool isShooting;
    bool isThrowing;
    bool isTakingDamage;
    bool isInvincible;
    bool isFacingRight;

    bool hitSideRight;

    bool freezeInput;
    bool freezePlayer;
    bool freezeEverything;

    float slideTime;
    float slideTimeLength;

    float shootTime;
    float shootTimeLength;
    bool keyShootRelease;
    float keyShootReleaseTimeLength;

    // last animation played
    string lastAnimationName;

    // delay for ground check
    bool jumpStarted;

    // freeze/hide player on screen
    float playerColor;
    RigidbodyConstraints2D rb2dConstraints;

    public int currentHealth;
    public int maxHealth = 28;

    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float jumpSpeed = 3.7f;

    [Header("Slide Settings")]
    [SerializeField] float slideSpeed = 3.0f;
    [SerializeField] float slideDuration = 0.35f;

    [Header("Positions and Prefabs")]
    [SerializeField] Transform bulletShootPos;
    [SerializeField] Transform slideDustPos;
    [SerializeField] GameObject prefabSlideDust;

    void Awake()
    {
        // get handles to components
        animator = GetComponent<Animator>();
        box2d = GetComponent<BoxCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // sprite defaults to facing right
        isFacingRight = true;

        // start at full health
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        isGrounded = false;
        Color raycastColor;
        RaycastHit2D raycastHit;
        float raycastDistance = 0.025f;
        int layerMask = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("MagnetBeam");
        // ground check
        Vector3 box_origin = box2d.bounds.center;
        box_origin.y = box2d.bounds.min.y + (box2d.bounds.extents.y / 4f);
        Vector3 box_size = box2d.bounds.size;
        box_size.y = box2d.bounds.size.y / 4f;
        raycastHit = Physics2D.BoxCast(box_origin, box_size, 0f, Vector2.down, raycastDistance, layerMask);
        // player box colliding with ground layer (ignore if teleport descending)
        if (raycastHit.collider != null && gameObject.layer != LayerMask.NameToLayer("Teleport") && !jumpStarted)
        {
            isGrounded = true;
            // just landed from jumping/falling
            if (isJumping)
            {
                isJumping = false;
            }
        }
        // draw debug lines
        raycastColor = (isGrounded) ? Color.green : Color.red;
        Debug.DrawRay(box_origin + new Vector3(box2d.bounds.extents.x, 0), Vector2.down * (box2d.bounds.extents.y / 4f + raycastDistance), raycastColor);
        Debug.DrawRay(box_origin - new Vector3(box2d.bounds.extents.x, 0), Vector2.down * (box2d.bounds.extents.y / 4f + raycastDistance), raycastColor);
        Debug.DrawRay(box_origin - new Vector3(box2d.bounds.extents.x, box2d.bounds.extents.y / 4f + raycastDistance), Vector2.right * (box2d.bounds.extents.x * 2), raycastColor);
    }

    // Update is called once per frame
    void Update()
    {
        // taking damage from projectiles, touching enemies, or other environment objects
        if (isTakingDamage)
        {
            PlayAnimation("Player_Hit");
            return;
        }

        // player input and movement
        PlayerDebugInput();
        PlayerDirectionInput();
        PlayerJumpInput();
        PlayerShootInput();

        // animations and movement from input
        PlayerMovement();

        // fire selected weapon
        FireWeapon();
    }

    void PlayerDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(1);
        }
    }

    void PlayerDirectionInput()
    {
        if (!freezeInput)
        {
#if UNITY_STANDALONE
            // get keyboard input
            keyHorizontal = Input.GetAxis("Horizontal");
            keyVertical = Input.GetAxisRaw("Vertical");
#endif

#if UNITY_ANDROID || UNITY_IOS
            // get on-screen virtual input
            keyHorizontal = SimpleInput.GetAxisRaw("Horizontal");
            keyVertical = SimpleInput.GetAxisRaw("Vertical");
#endif
        }
    }

    void PlayerJumpInput()
    {
#if UNITY_STANDALONE
        // get keyboard input
        if (!freezeInput)
        {
            keyJump = Input.GetKeyDown(KeyCode.Space);
        }
#endif
    }

    void PlayerShootInput()
    {
#if UNITY_STANDALONE
        // get keyboard input
        if (!freezeInput)
        {
            keyShoot = Input.GetKey(KeyCode.C);
        }
#endif
    }

    void PlayerMovement()
    {
        // override speed may vary depending on state
        float speed = moveSpeed;

        // ladder climbing part
        if (isClimbing)
        {
            // removed for this demo
        }
        // slide time
        else if (isSliding)
        {
            // flag to exit sliding
            bool exitSlide = false;

            // do the collision checks
            bool isTouchingTop = SlideTopCollision();
            bool isTouchingFront = SlideFrontCollision();

            // get how long the slide has run for
            slideTimeLength = Time.time - slideTime;

            // player is pressing left
            if (keyHorizontal < 0)
            {
                if (isFacingRight)
                {
                    if (isTouchingTop)
                    {
                        // flip only if between grounds
                        Flip();
                    }
                    else
                    {
                        // changing direction exits the slide
                        exitSlide = true;
                    }
                }
            }
            // player is pressing right
            else if (keyHorizontal > 0)
            {
                if (!isFacingRight)
                {
                    if (isTouchingTop)
                    {
                        // flip only if between grounds
                        Flip();
                    }
                    else
                    {
                        // changing direction exits the slide
                        exitSlide = true;
                    }
                }
            }

            // pressing jump and not between grounds
            if (keyJump && !isTouchingTop)
            {
                exitSlide = true;
                // directly call the jump coroutine
                //   bypass the jump() wrapper's logic
                StartCoroutine(JumpCo());
            }

            // exit the slide if we hit ground in front but not above
            //   stay in the slide just for a little bit before we exit
            if (isTouchingFront && !isTouchingTop && slideTimeLength >= 0.1f)
            {
                exitSlide = true;
            }

            // last slide exit test
            //   the time has elapsed and there is no touching any ground above
            //   slide off a ledge (not grounded) or the exit slide flag is set
            if ((slideTimeLength >= slideDuration && !isTouchingTop) || !isGrounded || exitSlide)
            {
                isSliding = false;
            }
            else
            {
                // we're still sliding - apply velocity
                rb2d.velocity = new Vector2(slideSpeed * ((isFacingRight) ? 1f : -1f), rb2d.velocity.y);
            }
        }
        // not climbing on any ladders
        else
        {
            // left arrow key - moving left
            if (keyHorizontal < 0)
            {
                // facing right while moving left - flip
                if (isFacingRight)
                {
                    Flip();
                }
                // grounded play run animation
                if (isGrounded)
                {
                    // play run shoot or run animation
                    if (isShooting)
                    {
                        PlayAnimation("Player_RunShoot");
                    }
                    else if (isThrowing)
                    {
                        speed = 0f;
                        PlayAnimation("Player_Throw");
                    }
                    else
                    {
                        PlayAnimation("Player_Run");
                    }
                }
            }
            else if (keyHorizontal > 0) // right arrow key - moving right
            {
                // facing left while moving right - flip
                if (!isFacingRight)
                {
                    Flip();
                }
                // grounded play run animation
                if (isGrounded)
                {
                    // play run shoot or run animation
                    if (isShooting)
                    {
                        PlayAnimation("Player_RunShoot");
                    }
                    else if (isThrowing)
                    {
                        speed = 0f;
                        PlayAnimation("Player_Throw");
                    }
                    else
                    {
                        PlayAnimation("Player_Run");
                    }
                }
            }
            else   // no movement
            {
                // grounded play idle animation
                if (isGrounded)
                {
                    // play shoot or idle animation
                    if (isShooting)
                    {
                        PlayAnimation("Player_Shoot");
                    }
                    else if (isThrowing)
                    {
                        PlayAnimation("Player_Throw");
                    }
                    else
                    {
                        PlayAnimation("Player_Idle");
                    }
                }
            }

            // move speed * direction (no movement zero x velocity)
            rb2d.velocity = new Vector2(speed * keyHorizontal, rb2d.velocity.y);

            // pressing jump while grounded - can only jump once
            Jump();

            // while not grounded play jump animation (jumping or falling)
            if (!isGrounded)
            {
                // triggers jump landing sound effect in FixedUpdate
                isJumping = true;
                // jump or jump shoot animation
                if (isShooting)
                {
                    PlayAnimation("Player_JumpShoot");
                }
                else if (isThrowing)
                {
                    PlayAnimation("Player_JumpThrow");
                }
                else
                {
                    PlayAnimation("Player_Jump");
                }
            }

            // start sliding here
            StartSliding();
        }
    }

    void Flip()
    {
        // invert facing direction and rotate object 180 degrees on y axis
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    void Jump()
    {
        // pressing jump while grounded and a jump hasn't started yet
        if (keyJump && isGrounded && !jumpStarted && keyVertical >= 0)
        {
            // call the jump coroutine below
            StartCoroutine(JumpCo());
        }
    }

    private IEnumerator JumpCo()
    {
        // jump velocity - lift off
        rb2d.velocity = new Vector2(rb2d.velocity.x, jumpSpeed);
        // delay to give time leaving the ground
        jumpStarted = true;
        yield return new WaitForSeconds(Time.fixedDeltaTime);
        jumpStarted = false;
    }

    void StartSliding()
    {
        // pressing down + jump when grounded will lead to sliding
        if (keyVertical < 0 && keyJump && isGrounded && !isSliding)
        {
            // there should be no ground directly in front of our slide
            if (!SlideFrontCollision())
            {
                // get the slide start time and play the animation
                isSliding = true;
                slideTime = Time.time;
                slideTimeLength = 0;
                PlayAnimation("Player_Slide");

                // add the slide dust visual effect
                GameObject slideDust = Instantiate(prefabSlideDust);
                slideDust.name = prefabSlideDust.name;
                slideDust.transform.position = slideDustPos.position;
                // default sprite is for sliding right so flip it if going left
                if (!isFacingRight)
                {
                    slideDust.transform.Rotate(0f, 180f, 0f);
                }
            }
        }
    }

    bool SlideFrontCollision()
    {
        // front box for frontal check
        Vector3 size = new Vector3(0.04f, 0.1f);
        Vector3 center = new Vector3(0, box2d.bounds.min.y + 0.02f + size.y / 2f);
        float offset = isSliding ? 0f : size.x / 2f;
        center.x = isFacingRight ? box2d.bounds.max.x + offset : box2d.bounds.min.x - offset;

        // see the box
        DrawBox(center, size, Color.yellow);

        // is the box touching ground
        return Physics2D.OverlapBox(center, size, 0, 1 << LayerMask.NameToLayer("Ground"));
    }

    bool SlideTopCollision()
    {
        // top box for overhead check
        Vector3 size = new Vector3(box2d.size.x - 0.02f, 0.1f);
        Vector3 center = new Vector3(0, box2d.bounds.max.y + size.y / 2f);
        center.x = isFacingRight ? box2d.bounds.center.x - 0.02f : box2d.bounds.center.x + 0.02f;

        // see the box
        DrawBox(center, size, Color.yellow);

        // is the box touching ground
        return Physics2D.OverlapBox(center, size, 0, 1 << LayerMask.NameToLayer("Ground"));
    }

    void DrawBox(Vector3 center, Vector3 size, Color color)
    {
        // visualize a box for debugging
        Debug.DrawLine(new Vector3(center.x - size.x / 2f, center.y + size.y / 2f),
            new Vector3(center.x + size.x / 2f, center.y + size.y / 2f), color);
        Debug.DrawLine(new Vector3(center.x - size.x / 2f, center.y - size.y / 2f),
            new Vector3(center.x + size.x / 2f, center.y - size.y / 2f), color);
        Debug.DrawLine(new Vector3(center.x - size.x / 2f, center.y - size.y / 2f),
            new Vector3(center.x - size.x / 2f, center.y + size.y / 2f), color);
        Debug.DrawLine(new Vector3(center.x + size.x / 2f, center.y - size.y / 2f),
            new Vector3(center.x + size.x / 2f, center.y + size.y / 2f), color);
    }

    void PlayAnimation(string animationName, int layer = -1, float normalizedTime = float.NegativeInfinity)
    {
        // allow our animations to play through from repeated calls
        if (animationName != lastAnimationName)
        {
            lastAnimationName = animationName;
            animator.Play(animationName, layer, normalizedTime);
        }
    }

    public bool IsGrounded()
    {
        // player's grounded status
        return isGrounded;
    }

    public bool IsInvincible()
    {
        // player's invincibility status
        return isInvincible;
    }

    void FireWeapon()
    {
        MegaBuster();
    }

    void MegaBuster()
    {
        shootTimeLength = 0;
        keyShootReleaseTimeLength = 0;

        // shoot key is being pressed and key release flag true
        if (keyShoot && keyShootRelease)
        {
            isShooting = true;
            keyShootRelease = false;
            shootTime = Time.time;
            // Shoot Bullet
            Invoke("ShootBullet", 0.1f);
        }
        // shoot key isn't being pressed and key release flag is false
        if (!keyShoot && !keyShootRelease)
        {
            keyShootReleaseTimeLength = Time.time - shootTime;
            keyShootRelease = true;
        }
        // while shooting limit its duration
        if (isShooting)
        {
            shootTimeLength = Time.time - shootTime;
            if (shootTimeLength >= 0.25f || keyShootReleaseTimeLength >= 0.15f)
            {
                isShooting = false;
            }
        }
    }

    void ShootBullet()
    {
        Debug.Log("Bang Bang!");
    }

    public void HitSide(bool rightSide)
    {
        // determines the push direction of the hit animation
        hitSideRight = rightSide;
    }

    public void Invincible(bool invincibility)
    {
        isInvincible = invincibility;
    }

    public void TakeDamage(int damage)
    {
        // take damage if not invincible
        if (!isInvincible)
        {
            // take damage amount from health and update the health bar
            if (damage > 0)
            {
                currentHealth -= damage;
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                StartDamageAnimation();
            }
        }
    }

    void StartDamageAnimation()
    {
        // once isTakingDamage is true in the Update function we'll play the Hit animation
        // here we go invincible so we don't repeatedly take damage, determine the X push force
        // depending which side we were hit on, and then apply that force
        if (!isTakingDamage)
        {
            isTakingDamage = true;
            Invincible(true);
            FreezeInput(true);
            if (!isSliding)
            {
                // when not sliding do the original code
                float hitForceX = 0.50f;
                float hitForceY = 1.5f;
                if (hitSideRight) hitForceX = -hitForceX;
                rb2d.velocity = Vector2.zero;
                rb2d.AddForce(new Vector2(hitForceX, hitForceY), ForceMode2D.Impulse);
            }
            else
            {
                // when sliding save the rigidbody constraints and freeze all
                rb2dConstraints = rb2d.constraints;
                rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }
    }

    void StopDamageAnimation()
    {
        // this function is called at the end of the Hit animation
        // and we reset the animation because it doesn't loop otherwise
        // we can end up stuck in it
        isTakingDamage = false;
        FreezeInput(false);
        PlayAnimation("Player_Hit", -1, 0f);
        if (isSliding)
        {
            // return to the slide animation and restore rigidbody constraints
            PlayAnimation("Player_Slide");
            rb2d.constraints = rb2dConstraints;
        }
        StartCoroutine(FlashAfterDamage());
    }

    private IEnumerator FlashAfterDamage()
    {
        // hit animation is 12 samples
        // keep flashing consistent with 1/12 secs
        float flashDelay = 0.0833f;
        // get sprite's current material
        //Material material = sprite.material;
        // toggle transparency
        for (int i = 0; i < 10; i++)
        {
            //sprite.enabled = false;
            //sprite.material = null;
            //sprite.material.SetFloat("_Transparency", 0f);
            //sprite.color = new Color(1, 1, 1, 0);
            sprite.color = Color.clear;
            yield return new WaitForSeconds(flashDelay);
            //sprite.enabled = true;
            //sprite.material = material; // new Material(Shader.Find("Sprites/Default"));
            //sprite.material.SetFloat("_Transparency", 1f);
            //sprite.color = new Color(1, 1, 1, 1);
            sprite.color = Color.white;
            yield return new WaitForSeconds(flashDelay);
        }
        // no longer invincible
        Invincible(false);
    }

    public void FreezeInput(bool freeze)
    {
        // freeze/unfreeze user input
        freezeInput = freeze;
        if (freeze)
        {
            keyHorizontal = 0;
            keyVertical = 0;
            keyJump = false;
            keyShoot = false;
        }
    }

    public void FreezePlayer(bool freeze)
    {
        // freeze/unfreeze the player on screen
        // zero animation speed and freeze XYZ rigidbody constraints
        if (freeze)
        {
            freezePlayer = true;
            rb2dConstraints = rb2d.constraints;
            animator.speed = 0;
            rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            freezePlayer = false;
            animator.speed = 1;
            rb2d.constraints = rb2dConstraints;
        }
    }
}