using UnityEngine;


public class SinScale : MonoBehaviour
{
    public float speed = 10.0f;
    public float power = 0.1f;

    private float updateTime = 0.0f;

    private void Start()
    {
        updateTime = Random.value * speed;
    }

    private void Update()
    {
        updateTime += Time.deltaTime * speed;
        Vector3 scale = transform.localScale;
        scale.y = 1.0f + (Mathf.Sin(updateTime) * power);
        transform.localScale = scale;
    }
}
