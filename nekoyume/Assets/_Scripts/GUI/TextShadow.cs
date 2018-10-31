using UnityEngine;
using UnityEngine.UI;


public class TextShadow : MonoBehaviour
{
    public enum Type : int
    {
        OFF,
        DIRECTION4,
        DIRECTION8,
    }

    public Type type;
    public Vector2Int distance;
    public Color shadowColor;

    private Text mainText;
    private RectTransform mainRect;
    private Text[] shadows;

    public void Awake()
    {
        mainText = GetComponent<Text>();
        mainRect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        DropShadow(type, distance);
    }

    public string Text
    {
        get
        {
            return mainText.text;
        }
        set
        {
            mainText.text = value;
            foreach (Text shadow in shadows)
            {
                shadow.text = value;
            }
        }
    }

    private void DropShadow(Type type, Vector2Int distance)
    {
        this.type = type;
        this.distance = distance;
        if (type == Type.OFF)
            return;

        // TODO
        CreateShadow(-distance.x, -distance.y);

        if (type == Type.DIRECTION4)
        {
            shadows = GetComponentsInChildren<Text>();
            return;
        }

        shadows = GetComponentsInChildren<Text>();
    }

    private void CreateShadow(int x, int y)
    {
        GameObject child = new GameObject();
        child.transform.parent = transform;
        child.name = string.Format("{0} {1}", x, y);

        Text text = child.AddComponent<Text>();
        text.font = mainText.font;
        text.fontSize = mainText.fontSize;
        text.text = mainText.text;
        text.color = shadowColor;

        RectTransform rect = child.AddComponent<RectTransform>();
        // TODO
    }
}
