using UnityEngine;
using UnityEngine.UI;

public class RoundIndicator : MonoBehaviour
{
    [SerializeField] private Image[] dots;
    [SerializeField] private Sprite emptyDot;
    [SerializeField] private Sprite wonDot;

    public void SetWins(int wins)
    {
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].sprite = i < wins ? wonDot : emptyDot;
        }
    }
}
