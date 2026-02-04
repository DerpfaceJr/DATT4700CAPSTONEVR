using UnityEngine;
using TMPro;
using System.Collections;
using System.IO;

public class TextControl : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI resultText;
    public float displayTime = 2f;

    private Coroutine hideCoroutine;

    private void Start()
    {
        // #region agent log
        try
        {
            string logPath = @"c:\Users\joshu\Documents\DATT4700CAPSTONEVR\.cursor\debug.log";
            bool rtNull = (resultText == null);
            string name = gameObject != null ? gameObject.name.Replace("\"", "'") : "null";
            string line = "{\"location\":\"TextControl.cs:Start\",\"message\":\"Start\",\"data\":{\"resultTextIsNull\":" + (rtNull ? "true" : "false") + ",\"gameObjectName\":\"" + name + "\"},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ",\"sessionId\":\"debug-session\",\"hypothesisId\":\"A\"}\n";
            File.AppendAllText(logPath, line);
        }
        catch (System.Exception) { }
        // #endregion
        if (resultText == null)
            return;
        resultText.gameObject.SetActive(false);
    }

    /* =========================================================
       * PROJECTILE — TRIGGER BASED
       * ========================================================= */

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && gameObject.CompareTag("Sword") != true)
        {
            ShowText("Hit", Color.green);
        }

        //Command out for now

        //else if (collision.gameObject.CompareTag("Ground"))
        //{
        //    ShowText("Miss", Color.red);
        //}
    }

    /* =========================================================
       * MELEE — TRIGGER BASED (HELD SWORD)
       * ========================================================= */
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy") && gameObject.CompareTag("Sword"))
        {
            ShowText("Hit", Color.green);
        }
    }

    void ShowText(string message, Color color)
    {
        if (resultText == null)
            return;
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
        if (resultText != null)
            resultText.gameObject.SetActive(false);
    }
}

