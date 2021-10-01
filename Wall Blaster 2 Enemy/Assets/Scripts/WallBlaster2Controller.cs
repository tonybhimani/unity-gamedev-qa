using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBlaster2Controller : MonoBehaviour
{
    Animator animator;
    BoxCollider2D box2d;
    SpriteRenderer sprite;
    Rigidbody2D rb2d;

    bool isFacingRight;
    bool isShooting;

    float shootTimer;
    float halfSpriteHeight;
    string lastAnimationName;

    GameObject player;
    Vector3 playerPosition;
    float playerHeight;

    // flag to enable ai
    public bool enableAI;

    // remove for your own audio engine
    // and update the audio code in ShootBullet()
    public AudioSource audioSource;

    // wall blaster props
    public float shootDelay = 1.5f;
    public float bulletSpeed = 2f;
    public float playerAttackRange = 3f;

    // wall blaster bullet props
    public AudioClip bulletClip;
    public Transform bulletShootPos;
    public GameObject bulletPrefab;

    public enum WallBlaster2Colors { RedGreen, GreenRed, GreenOrange };
    public WallBlaster2Colors wallBlaster2Color = WallBlaster2Colors.RedGreen;

    public enum WallBlaster2Orientations { Left, Right };
    public WallBlaster2Orientations wallBlaster2Orientation = WallBlaster2Orientations.Right;

    public enum WallBlaster2Angles { Center, Up, Down };
    public WallBlaster2Angles wallBlaster2Angle = WallBlaster2Angles.Center;

    [SerializeField] RuntimeAnimatorController racWallBlaster2RedGreen;
    [SerializeField] RuntimeAnimatorController racWallBlaster2GreenRed;
    [SerializeField] RuntimeAnimatorController racWallBlaster2GreenOrange;

    void Awake()
    {
        // get components
        animator = GetComponent<Animator>();
        box2d = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // set color and orientation
        SetColor(wallBlaster2Color);
        SetAngle(wallBlaster2Angle);
        SetOrientation(wallBlaster2Orientation);

        // switch facing direction
        isFacingRight = false;
        if (wallBlaster2Orientation == WallBlaster2Orientations.Left)
        {
            isFacingRight = true;
            Flip();
        }

        // init shoot delay
        shootTimer = shootDelay;

        // get half the sprite height
        halfSpriteHeight = sprite.bounds.size.y / 2;

        // play initial animation
        PlayAnimation(GetAnimationName());

        // get player object
        player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update()
    {
        // get player position and sprite height
        if (player != null)
        {
            playerPosition = player.transform.position;
            if (player.GetComponent<SpriteRenderer>() != null)
            {
                playerHeight = player.GetComponent<SpriteRenderer>().sprite.bounds.size.y;
            }
        }

        // testing - click the right mouse to shoot the bullet
        if (Input.GetMouseButtonDown(1))
        {
            Shoot();
        }

        // if enemy ai is enabled
        if (enableAI)
        {
            // get player distance
            float playerDistance = Vector2.Distance(playerPosition, transform.position);

            // player is in front of the cannon
            if (playerPosition.x > bulletShootPos.position.x && isFacingRight ||
                playerPosition.x < bulletShootPos.position.x && !isFacingRight)
            {
                // don't change the angle or prepare to shoot while already shooting
                if (!isShooting)
                {
                    // player vertical relation (default to center)
                    wallBlaster2Angle = WallBlaster2Angles.Center;
                    // test for upward angle
                    if (playerPosition.y > transform.position.y + halfSpriteHeight)
                    {
                        wallBlaster2Angle = WallBlaster2Angles.Up;
                    }
                    // test for downward angle
                    else if (playerPosition.y + playerHeight < transform.position.y - halfSpriteHeight)
                    {
                        wallBlaster2Angle = WallBlaster2Angles.Down;
                    }

                    // player is within the attack range
                    if (playerDistance <= playerAttackRange)
                    {
                        // countdown to the next shot
                        shootTimer -= Time.deltaTime;
                        if (shootTimer <= 0)
                        {
                            Shoot();
                        }
                    }
                }
            }

            // play the animation
            PlayAnimation(GetAnimationName());
        }
    }

    void Flip()
    {
        // flip the transform to rotate
        transform.Rotate(0, 180f, 0);
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

    public void EnableAI(bool enable)
    {
        // enable enemy ai
        this.enableAI = enable;
    }

    public void SetColor(WallBlaster2Colors color)
    {
        // set color
        this.wallBlaster2Color = color;
        SetAnimatorController();
    }

    public void SetAngle(WallBlaster2Angles angle)
    {
        // set angle
        this.wallBlaster2Angle = angle;
    }

    public void SetOrientation(WallBlaster2Orientations orientation)
    {
        // set orientation
        this.wallBlaster2Orientation = orientation;
    }

    public void SetShootDelay(float delay)
    {
        // set the shoot delay
        this.shootDelay = delay;
    }

    public void SetBulletSpeed(float speed)
    {
        // set the bullet speed
        this.bulletSpeed = speed;
    }

    public void SetPlayerAttackRange(float range)
    {
        // set the player attack range
        this.playerAttackRange = range;
    }

    void SetAnimatorController()
    {
        // set animator controller from color
        switch (wallBlaster2Color)
        {
            case WallBlaster2Colors.RedGreen:
                animator.runtimeAnimatorController = racWallBlaster2RedGreen;
                break;
            case WallBlaster2Colors.GreenRed:
                animator.runtimeAnimatorController = racWallBlaster2GreenRed;
                break;
            case WallBlaster2Colors.GreenOrange:
                animator.runtimeAnimatorController = racWallBlaster2GreenOrange;
                break;
        }
    }

    public void Shoot()
    {
        // one shot at a time
        if (!isShooting)
        {
            isShooting = true;
            PlayAnimation(GetAnimationName(), 0, 0);
        }
    }

    void ShootBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab);
        bullet.name = "Bullet";
        bullet.transform.position = bulletShootPos.position;
        bullet.GetComponent<BulletScript>().SetSpeed(this.bulletSpeed);
        bullet.GetComponent<BulletScript>().SetDirection(GetBulletVector());
        // bullet audio clip
        audioSource.time = 0;
        audioSource.loop = false;
        audioSource.clip = bulletClip;
        audioSource.Play();
    }

    void ShootAnimationEnd()
    {
        // called at the end of the shoot animation
        if (isShooting)
        {
            isShooting = false;
            shootTimer = shootDelay;
            PlayAnimation(GetAnimationName());
        }
    }

    string GetAnimationName()
    {
        // make the animation name
        string action = isShooting ? "Shoot" : "Idle";
        string angle = ((int)wallBlaster2Angle + 1).ToString();
        string animationName = "WallBlaster2_" + action + angle;
        // animation name string
        return animationName;
    }

    Vector2 GetBulletVector()
    {
        // default vector
        Vector2 bulletVector = new Vector2(-1f, 0);
        // new vector if at angle
        switch (wallBlaster2Angle)
        {
            case WallBlaster2Angles.Up:
                bulletVector = new Vector2(-0.75f, 0.75f);
                break;
            case WallBlaster2Angles.Down:
                bulletVector = new Vector2(-0.75f, -0.75f);
                break;
        }
        // invert for other direction
        if (isFacingRight) bulletVector.x *= -1f;
        // done with vector
        return bulletVector;
    }
}