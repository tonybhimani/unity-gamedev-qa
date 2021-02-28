using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    Rigidbody2D rb2d;

    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float jumpSpeed = 3.7f;

    float keyHorizontal;
    bool keyJump;
    bool keyShoot;

    bool isGrounded;
    bool isJumping;
    bool isShooting;
    bool isFacingRight;

    float shootTime;
    bool keyShootRelease;

    [Header("Accelerated Running")]
    // vars for our accelerated running
    [SerializeField] int maxGearShifts = 5;
    [SerializeField] float gearShiftDelay = 0.5f;
    [SerializeField] float accelerationIncrease = 1f;
    int currentGearShift;
    float gearShiftTimer;
    float acceleration;
    float accelerationBase;
    float camTimeOffset;

    // see the velocity on screen
    Text speedText;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        box2d = GetComponent<BoxCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();

        // sprite defaults to facing right
        isFacingRight = true;

        // get camera time offset
        camTimeOffset = Camera.main.GetComponent<CameraFollow>().timeOffset;

        // get speed text object
        speedText = GameObject.Find("SpeedText").GetComponent<Text>();
    }

    private void FixedUpdate()
    {
        isGrounded = false;
        Color raycastColor;
        RaycastHit2D raycastHit;
        float raycastDistance = 0.05f;
        int layerMask = 1 << LayerMask.NameToLayer("Ground");
        // ground check
        Vector3 box_origin = box2d.bounds.center;
        box_origin.y = box2d.bounds.min.y + (box2d.bounds.extents.y / 4f);
        Vector3 box_size = box2d.bounds.size;
        box_size.y = box2d.bounds.size.y / 4f;
        raycastHit = Physics2D.BoxCast(box_origin, box_size, 0f, Vector2.down, raycastDistance, layerMask);
        // player box colliding with ground layer
        if (raycastHit.collider != null)
        {
            isGrounded = true;
            // just landed from jumping/falling
            if (isJumping)
            {
                isJumping = false;
                // stop accelerated running
                StopAcceleratedRunning();
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
        PlayerDirectionInput();
        PlayerShootInput();
        PlayerMovement();

        // look how fast he can go!
        PlayerAcceleratedRunning();

        // update speed text with current X velocity
        speedText.text = String.Format("Speed {0:0.00}", Mathf.Abs(rb2d.velocity.x));
    }

    void PlayerDirectionInput()
    {
        // get keyboard input
        keyHorizontal = Input.GetAxisRaw("Horizontal");
        keyJump = Input.GetKeyDown(KeyCode.Space);
        keyShoot = Input.GetKey(KeyCode.C);
    }

    void PlayerShootInput()
    {
        float shootTimeLength = 0;
        float keyShootReleaseTimeLength = 0;

        // shoot key is being pressed and key release flag true
        if (keyShoot && keyShootRelease)
        {
            isShooting = true;
            keyShootRelease = false;
            shootTime = Time.time;
            // Shoot Bullet
            Debug.Log("Shoot Bullet");
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

    void PlayerMovement()
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
                    animator.Play("Player_RunShoot");
                }
                else
                {
                    animator.Play("Player_Run");
                }
            }
            // negative move speed to go left
            rb2d.velocity = new Vector2(-(moveSpeed + acceleration), rb2d.velocity.y);
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
                    animator.Play("Player_RunShoot");
                }
                else
                {
                    animator.Play("Player_Run");
                }
            }
            // positive move speed to go right
            rb2d.velocity = new Vector2(moveSpeed + acceleration, rb2d.velocity.y);
        }
        else   // no movement
        {
            // grounded play idle animation
            if (isGrounded)
            {
                // play shoot or idle animation
                if (isShooting)
                {
                    animator.Play("Player_Shoot");
                }
                else
                {
                    animator.Play("Player_Idle");
                }
            }
            // no movement zero x velocity
            rb2d.velocity = new Vector2(0f, rb2d.velocity.y);
        }

        // pressing jump while grounded - can only jump once
        if (keyJump && isGrounded)
        {
            // play jump/jump shoot animation and jump speed on y velocity
            if (isShooting)
            {
                animator.Play("Player_JumpShoot");
            }
            else
            {
                animator.Play("Player_Jump");
            }
            rb2d.velocity = new Vector2(rb2d.velocity.x, jumpSpeed);
        }

        // while not grounded play jump animation (jumping or falling)
        if (!isGrounded)
        {
            // triggers jump landing sound effect in FixedUpdate
            isJumping = true;
            // jump or jump shoot animation
            if (isShooting)
            {
                animator.Play("Player_JumpShoot");
            }
            else
            {
                animator.Play("Player_Jump");
            }
        }
    }

    void PlayerAcceleratedRunning()
    {
        // check for being grounded and left or right arrow keys being pressed
        if (isGrounded && keyHorizontal != 0)
        {
            // only increase speed if we haven't maxed out on gear shifts
            if (currentGearShift < maxGearShifts)
            {
                // smooth acceleration increases
                float progress = Mathf.Clamp(gearShiftTimer, 0, gearShiftDelay) / gearShiftDelay;
                acceleration = accelerationBase + (progress * accelerationIncrease);
                // increment timer between gear shifts
                gearShiftTimer += Time.deltaTime;
                if (progress >= 1f)
                {
                    // increase acceleration, increment gear shift, reset the timer
                    // double time our running animation and increase the camera time offset
                    // so it doesn't jitter and can keep up with the speed of player movement
                    accelerationBase += accelerationIncrease;
                    currentGearShift++;
                    gearShiftTimer = 0;
                    animator.speed = 2;
                    Camera.main.GetComponent<CameraFollow>().timeOffset = 1f;
                }
            }
        }
        else
        {
            // we stop acceleration if we're grounded however if in the middle of a jump then we want to 
            // keep the acceleration - FixedUpdate will reset the acceleration upon landing on the ground
            if (!isJumping)
            {
                StopAcceleratedRunning();
            }
        }
    }

    void StopAcceleratedRunning()
    {
        // reset everything back to default
        currentGearShift = 0;
        acceleration = 0;
        accelerationBase = 0;
        gearShiftTimer = 0;
        animator.speed = 1;
        Camera.main.GetComponent<CameraFollow>().timeOffset = camTimeOffset;
    }

    void Flip()
    {
        // invert facing direction and rotate object 180 degrees on y axis
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }
}
