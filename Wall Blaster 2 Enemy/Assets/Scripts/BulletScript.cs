using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    BoxCollider2D box2d;
    SpriteRenderer sprite;
    Rigidbody2D rb2d;

    // bullet properties
    [SerializeField] float bulletSpeed = 1f;
    [SerializeField] Vector2 bulletDirection = new Vector2(1f, 0);

    void Awake()
    {
        // get components
        box2d = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // destroy after 5 seconds
        Destroy(gameObject, 5f);
    }

    // Update is called once per frame
    void Update()
    {
        // apply speed and direction
        rb2d.velocity = this.bulletSpeed * this.bulletDirection;
    }

    public void SetSpeed(float speed)
    {
        // set bullet speed
        this.bulletSpeed = speed;
    }

    public void SetDirection(Vector2 direction)
    {
        // set bullet direction
        this.bulletDirection = direction;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // check collision with Player tag
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Bullet Hit Player");
            Destroy(gameObject);
        }
    }
}
