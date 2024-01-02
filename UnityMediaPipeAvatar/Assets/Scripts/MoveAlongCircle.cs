using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAlongCircle : MonoBehaviour
{
    public float speed = 2;
    public float radius = 3;

    float timer;
    Vector3 i;

    private void Start()
    {
        i = transform.position;
    }

    private void LateUpdate()
    {
        Vector2 v = new Vector2(
            Mathf.Cos(timer) * radius,
            Mathf.Sin(timer) * radius);

        transform.position = i+new Vector3(v.x,0,v.y);
        transform.rotation = Quaternion.LookRotation(new Vector3(v.x, 0, v.y), Vector3.up);

        timer += Time.deltaTime*speed;
    }

}
