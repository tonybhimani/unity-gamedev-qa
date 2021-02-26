using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooter : MonoBehaviour
{
    int bulletIndex;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] BulletScript.BulletTypes bulletType;

    // Start is called before the first frame update
    void Start()
    {
        // default to the first bullet
        bulletType = BulletScript.BulletTypes.Bullet1;
        bulletIndex = (int)bulletType;

        // shooting object we're on
        ShowShootingObjectName();
    }

    // Update is called once per frame
    void Update()
    {
        // create and shoot the object with left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.name = bulletPrefab.name;
            bullet.transform.position = transform.position;
            bullet.GetComponent<BulletScript>().SetBulletSpeed(1f);
            bullet.GetComponent<BulletScript>().SetBulletDirection(Vector2.right);
            bullet.GetComponent<BulletScript>().SetBulletType(bulletType);
            bullet.GetComponent<BulletScript>().SetDestroyDelay(5f);
            bullet.GetComponent<BulletScript>().Shoot();
            // make it bigger to see it better
            bullet.transform.localScale *= 2f;
        }

        // change shooting object via right mouse click
        if (Input.GetMouseButtonDown(1))
        {
            var bulletArray = Enum.GetValues(typeof(BulletScript.BulletTypes));
            if (++bulletIndex > (int)bulletArray.Length - 1)
            {
                bulletIndex = 0;
            }
            bulletType = (BulletScript.BulletTypes)bulletIndex;

            // shooting object we're on
            ShowShootingObjectName();
        }
    }

    private void ShowShootingObjectName()
    {
        // show the shooting object name on the console
        Debug.Log("Shooting Object: " + Enum.GetName(typeof(BulletScript.BulletTypes), bulletIndex));
    }
}
