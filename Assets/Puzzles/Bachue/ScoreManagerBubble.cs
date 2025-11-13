using TMPro;
using UnityEngine;

public class ScoreManagerBubble : MonoBehaviour
{
    [Header("UI")]
    public TextMeshPro scoreText; // si usas TextMeshPro - 3D
    // public TextMeshProUGUI scoreTextUI; // si usas Canvas World Space

    int score = 0;

    void Start()
    {
        UpdateText();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateText();
    }

    void UpdateText()
    {
        if (scoreText != null)
            scoreText.text = "Puntaje: " + score;
    }
}
