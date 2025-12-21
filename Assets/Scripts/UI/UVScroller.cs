using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UVScroller : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.5f, 0f);

    private RawImage raw;
    private Rect uvRect;

    void Awake()
    {
        raw = GetComponent<RawImage>();
        uvRect = raw.uvRect;
    }

    void Update()
    {
        uvRect.x += speed.x * Time.unscaledDeltaTime;
        uvRect.y += speed.y * Time.unscaledDeltaTime;
        raw.uvRect = uvRect;
    }
}
