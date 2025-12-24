using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootSolver : MonoBehaviour
{
    public bool isMovingForward;

    [SerializeField] LayerMask terrainLayer = default;
    [SerializeField] Transform body = default;
    [SerializeField] IKFootSolver otherFoot = default;
    [SerializeField] float speed = 4;
    [SerializeField] float stepDistance = .2f;
    [SerializeField] float stepLength = .2f;
    [SerializeField] float sideStepLength = .1f;

    [SerializeField] float stepHeight = .3f;
    [SerializeField] Vector3 footOffset = default;

    public Vector3 footRotOffset;
    public float footYPosOffset = 0.1f;

    public float rayStartYOffset = 0;
    public float rayLength = 1.5f;
    
    float footSpacing;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    float lerp;

    private void Start()
    {
        // #region agent log
        string log1 = "{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A,C,D\",\"location\":\"IKFootSolver.cs:Start\",\"message\":\"IKFootSolver Start\",\"data\":{\"gameObjectName\":\"" + gameObject.name + "\",\"bodyIsNull\":" + (body == null ? "true" : "false") + ",\"bodyName\":\"" + (body != null ? body.name : "null") + "\",\"transformName\":\"" + transform.name + "\",\"parentName\":\"" + (transform.parent != null ? transform.parent.name : "null") + "\"},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        System.IO.File.AppendAllText(@"c:\Users\joshu\Documents\DATT4700CAPSTONEVR\.cursor\debug.log", log1);
        // #endregion
        footSpacing = transform.localPosition.x;
        currentPosition = newPosition = oldPosition = transform.position;
        currentNormal = newNormal = oldNormal = transform.up;
        lerp = 1;
        
        // #region agent log
        string log2 = "{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\",\"location\":\"IKFootSolver.cs:Start\",\"message\":\"Attempting auto-assign body\",\"data\":{\"bodyWasNull\":" + (body == null ? "true" : "false") + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        System.IO.File.AppendAllText(@"c:\Users\joshu\Documents\DATT4700CAPSTONEVR\.cursor\debug.log", log2);
        // #endregion
        // Try to auto-assign body if null (Hypothesis D)
        if (body == null)
        {
            // Try to find character root (parent hierarchy)
            Transform current = transform.parent;
            int depth = 0;
            while (current != null && depth < 10)
            {
                // #region agent log
                string log3 = "{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\",\"location\":\"IKFootSolver.cs:Start\",\"message\":\"Searching for body\",\"data\":{\"currentName\":\"" + current.name + "\",\"depth\":" + depth + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                System.IO.File.AppendAllText(@"c:\Users\joshu\Documents\DATT4700CAPSTONEVR\.cursor\debug.log", log3);
                // #endregion
                // Look for character root (usually has Animator or is named Ch44)
                if (current.GetComponent<Animator>() != null || current.name.Contains("Ch44") || current.name.Contains("Enemy"))
                {
                    body = current;
                    // #region agent log
                    string log4 = "{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"D\",\"location\":\"IKFootSolver.cs:Start\",\"message\":\"Auto-assigned body\",\"data\":{\"bodyName\":\"" + body.name + "\"},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                    System.IO.File.AppendAllText(@"c:\Users\joshu\Documents\DATT4700CAPSTONEVR\.cursor\debug.log", log4);
                    // #endregion
                    break;
                }
                current = current.parent;
                depth++;
            }
        }
    }

    // Update is called once per frame

    void Update()
    {
        // #region agent log
        string log5 = "{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\",\"location\":\"IKFootSolver.cs:Update\",\"message\":\"Update entry\",\"data\":{\"bodyIsNull\":" + (body == null ? "true" : "false") + ",\"gameObjectName\":\"" + gameObject.name + "\"},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
        System.IO.File.AppendAllText(@"c:\Users\joshu\Documents\DATT4700CAPSTONEVR\.cursor\debug.log", log5);
        // #endregion
        // Check if body is null (Hypothesis C) - prevent crash
        if (body == null)
        {
            // #region agent log
            string log6 = "{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C,B\",\"location\":\"IKFootSolver.cs:Update\",\"message\":\"Body is null, skipping Update\",\"data\":{\"gameObjectName\":\"" + gameObject.name + "\",\"isEnemy\":" + gameObject.name.Contains("Enemy").ToString().ToLower() + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            System.IO.File.AppendAllText(@"c:\Users\joshu\Documents\DATT4700CAPSTONEVR\.cursor\debug.log", log6);
            // #endregion
            // Disable component if body is null (enemies don't need foot IK)
            enabled = false;
            return;
        }
        
        transform.position = currentPosition + Vector3.up * footYPosOffset;
        transform.localRotation = Quaternion.Euler(footRotOffset);

        Ray ray = new Ray(body.position + (body.right * footSpacing) + Vector3.up * rayStartYOffset, Vector3.down);

        Debug.DrawRay(body.position + (body.right * footSpacing) + Vector3.up * rayStartYOffset, Vector3.down);
            
        if (Physics.Raycast(ray, out RaycastHit info, rayLength, terrainLayer.value))
        {
            if (Vector3.Distance(newPosition, info.point) > stepDistance && !otherFoot.IsMoving() && lerp >= 1)
            {
                lerp = 0;
                Vector3 direction = Vector3.ProjectOnPlane(info.point - currentPosition,Vector3.up).normalized;

                float angle = Vector3.Angle(body.forward, body.InverseTransformDirection(direction));

                isMovingForward = angle < 50 || angle > 130;

                if(isMovingForward)
                {
                    newPosition = info.point + direction * stepLength + footOffset;
                    newNormal = info.normal;
                }
                else
                {
                    newPosition = info.point + direction * sideStepLength + footOffset;
                    newNormal = info.normal;
                }

            }
        }

        if (lerp < 1)
        {
            Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
            tempPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

            currentPosition = tempPosition;
            currentNormal = Vector3.Lerp(oldNormal, newNormal, lerp);
            lerp += Time.deltaTime * speed;
        }
        else
        {
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
    }

    private void OnDrawGizmos()
    {

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.1f);
    }



    public bool IsMoving()
    {
        return lerp < 1;
    }



}
