using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Effect : MonoBehaviour
{
    static private int DEFAULT_LAYER = 15;
    static public EffectPool Pool = null;

    private SpriteRenderer renderer;
    private float fps = 24;
    private int currentFrame = 0;
    private float updateTime = 0.0f;
    private bool playing = false;
    private Sprite[] sprites = null;

    static public void Show(string name, Vector3 position)
    {
        GameObject go = Pool.Get();
        if (go != null)
        {
            Effect effect = go.GetComponent<Effect>();
            effect.Play(name, position);
        }
    }

    private void Awake()
    {
        renderer = gameObject.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = DEFAULT_LAYER;
    }
    
    private void Update()
    {
        if (playing)
        {
            updateTime += Time.deltaTime;
            float t = 1.0f / fps;
            if (updateTime >= t)
            {
                updateTime -= t;
                NextFrame();
            }
        }
    }

    public void Play(string name, Vector3 position)
    {
        updateTime = 0.0f;
        transform.position = position;
        playing = true;
        sprites = Resources.LoadAll<Sprite>(string.Format("images/{0}", name));
        SetFrame(0);
    }

    private void NextFrame()
    {
        SetFrame(currentFrame + 1);
    }

    private void SetFrame(int frame)
    {
        if (sprites.Length > frame)
        {
            currentFrame = frame;
            renderer.sprite = sprites[currentFrame];
        }
        else
        {
            currentFrame = 0;
            renderer.sprite = null;
            playing = false;
            gameObject.SetActive(false);
        }
    }
}
