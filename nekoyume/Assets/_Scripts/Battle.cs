using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;


[Serializable]
public class BattleStatus
{
    // common
    public string type = "";
    public string id_ = "";
    public int time = 0;

    public string name = "";

    // spawn
    public string class_ = "";
    public int character_type = 0;
    public int level = 0;
    public int hp = 0;
    public int hp_max = 0;
    public string armor = "";
    public string head = "";
    public string weapon = "";

    // skill
    public string target_id = "";
    public int target_hp = 0;
    public int target_remain = 0;
    public int tick_remain = 0;

    // exp
    public int exp = 0;

    public Type GetStatusType()
    {
        string typestr = type.Split(new [] {"_"}, StringSplitOptions.RemoveEmptyEntries)
        .Select(s =>char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1))
        .Aggregate(string.Empty, (s1, s2) => s1 + s2);
        return Type.GetType(typestr);
    }
}


[Serializable]
public class BattleJson
{
    public BattleStatus[] status;
}


public class Battle : MonoBehaviour
{
    public Dictionary<string, GameObject> characters = new Dictionary<string, GameObject>();
    public GameObject background;
    public Transform[] groups;
    public GameObject homeCharacter;
    public TextAsset dummyJson;

    [DllImport("__Internal")]
    public static extern void OnLoadUnity();

    [DllImport("__Internal")]
    public static extern void OnMessage(string msg);

    [DllImport("__Internal")]
    public static extern void OnSkill(string name);

    private void Start()
    {
        #if !UNITY_EDITOR
        OnLoadUnity();
        #endif
    }

    private IEnumerator Simulation(string json)
    {
        var battleJson = JsonUtility.FromJson<BattleJson>(json);
        foreach (var battleStatus in battleJson.status)
        {
            var statusType = battleStatus.GetStatusType();
            if (statusType != null)
            {
                var status = (Status)System.Activator.CreateInstance(statusType);
                if (status != null)
                {
                    yield return status.Execute(this, battleStatus);
                }
            }
        }

        yield return new WaitForSeconds(1.0f);
        #if !UNITY_EDITOR
        OnMessage("end_battle");
        #endif
    }

    public void Play(string json)
    {
        Clear();
        if (string.IsNullOrEmpty(json))
        {
            json = dummyJson.text;
        }
        StartCoroutine(Simulation(json));
    }

    public void LoadJson(string url)
    {
        StartCoroutine(LoadUrl(url));
    }

    public IEnumerator LoadUrl(string url)
    {
        WWW www = new WWW(url);
        yield return www;
        Play(www.text);
    }

    public void Clear()
    {
        DOTween.Clear(true);
        StopAllCoroutines();

        SetBackground("");

        var bgScroller = background.GetComponent<BGScroller>();
        bgScroller.scrollSpeed = 0.0f;

        homeCharacter.SetActive(false);

        foreach (Transform group in groups)
        {
            for (int i = 0; i < group.childCount; ++i)
            {
                var child = group.GetChild(i);
                child.gameObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                var character = child.gameObject.GetComponent<Character>();
                character.Clear();
                character.hp = 0;

                var characterRenderer = child.gameObject.GetComponent<SpriteRenderer>();
                characterRenderer.sprite = null;

                var sinScale = child.gameObject.GetComponent<SinScale>();
                sinScale.enabled = true;
            }
        }
    }

    public void Home(string avatar)
    {
        Clear();

        var bgRenderer = background.GetComponent<SpriteRenderer>();
        bgRenderer.sprite = Resources.Load<Sprite>("images/bg_room");

        homeCharacter.SetActive(true);
        var anim = homeCharacter.GetComponent<Animator>();
        anim.Play("idle");
    }

    public void Walking(string avatar)
    {
        Clear();

        SetBackground("zone_0", 2.0f);

        homeCharacter.SetActive(true);
        var anim = homeCharacter.GetComponent<Animator>();
        anim.Play("walk");
    }

    public void Sleep(string avatar)
    {
        Clear();

        SetBackground("bg_room");
    }

    public void SetBackground(string name, float scrollSpeed = 0.0f)
    {
        background.transform.position = Vector3.zero;
        var bgRenderers = background.GetComponentsInChildren<SpriteRenderer>();
        foreach (var bgRenderer in bgRenderers)
            bgRenderer.sprite = Resources.Load<Sprite>(string.Format("images/{0}", name));

        var bgScroller = background.GetComponent<BGScroller>();
        bgScroller.scrollSpeed = scrollSpeed;
    }
}
