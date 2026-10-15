using UnityEngine;

public class ScrollTexture : MonoBehaviour
{
    [SerializeField] private Vector2 speed = new Vector2(0.02f, 0.01f);

    private Material material;

    private void Start()
    {
        material = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        material.mainTextureOffset += speed * Time.deltaTime;
    }
}
