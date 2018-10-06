using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;


public class Character : MonoBehaviour
{
    public ProgressBar hpBar = null;
    public ProgressBar castingBar = null;
    public int hp = 0;
    public int hpMax = 0;
    public float movePower = 1.0f;

    public void Start()
    {
        GameObject res = Resources.Load<GameObject>("Prefab/ProgressBar");
        GameObject canvas = GameObject.FindGameObjectWithTag("Canvas");

        GameObject hpObj = Instantiate(res, canvas.transform);
        hpObj.SetActive(false);
        hpBar = hpObj.GetComponent<ProgressBar>();

        GameObject castingObj = Instantiate(res, canvas.transform);
        castingObj.SetActive(false);
        castingBar = castingObj.GetComponent<ProgressBar>();
        castingBar.bar.color = Color.blue;
    }

    public void Clear()
    {
        hpBar.gameObject.SetActive(false);
        castingBar.gameObject.SetActive(false);
    }

    private void UpdateHp()
    {
        Slider slider = hpBar.gameObject.GetComponent<Slider>();
        slider.value = (float)hp / (float)hpMax;
        hpBar.label.text = string.Format("{0}/{1}", hp, hpMax);
    }

    public void Spawn()
    {
        hpBar.gameObject.SetActive(true);
        hpBar.UpdatePosition(gameObject);
        hpMax = hp;

        UpdateHp();
    }

    public bool IsDead()
    {
        return hp == 0;
    }

    public void Attack()
    {
        const float duration = 0.1f;

        Vector3 fromPosition = transform.position;
        Vector3 toPosition = fromPosition + new Vector3(0.4f * movePower, 0.0f, 0.0f);
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOJump(toPosition, 0.2f, 1, duration, false));
        seq.Append(transform.DOMove(fromPosition, duration));
    }

    public void Hit()
    {
        Effect.Show("hit_01", transform.position);

        const float duration = 0.1f;

        Vector3 fromPosition = transform.position;
        Vector3 toPosition = fromPosition + new Vector3(-0.1f * movePower, 0.0f, 0.0f);
        Sequence moveseq = DOTween.Sequence();
        moveseq.Append(transform.DOMove(toPosition, duration));
        moveseq.Append(transform.DOMove(fromPosition, duration));

        SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
        Material mat = renderer.material;
        Sequence colorseq = DOTween.Sequence();
        colorseq.Append(mat.DOColor(Color.red, duration));
        colorseq.Append(mat.DOColor(Color.white, duration));

        UpdateHp();
    }

    public void Heal()
    {
        Effect.Show("impact_01", transform.position);

        const float duration = 0.1f;

        SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
        Material mat = renderer.material;
        Sequence colorseq = DOTween.Sequence();
        colorseq.Append(mat.DOColor(Color.yellow, duration));
        colorseq.Append(mat.DOColor(Color.white, duration));

        UpdateHp();
    }
}
