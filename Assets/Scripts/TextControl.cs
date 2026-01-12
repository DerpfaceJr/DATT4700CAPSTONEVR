using UnityEngine;
using TMPro;
using System.Collections;

public class TextControl : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI resultText;
    public float displayTime = 2f;

    private Coroutine hideCoroutine;

    private void Start()
    {
        resultText.gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            ShowText("Hit", Color.green);
        }
        else if (collision.gameObject.CompareTag("Ground") ||
                 collision.gameObject.CompareTag("Wall"))
        {
            ShowText("Miss", Color.red);
        }
    }

    void ShowText(string message, Color color)
    {
        resultText.text = message;
        resultText.color = color;
        resultText.gameObject.SetActive(true);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideTextAfterTime());
    }

    IEnumerator HideTextAfterTime()
    {
        yield return new WaitForSeconds(displayTime);
        resultText.gameObject.SetActive(false);
    }
}
