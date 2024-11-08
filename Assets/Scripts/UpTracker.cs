using UnityEngine;

public class UpTracker : MonoBehaviour
{
    public Transform rightShoe;
    public Transform leftShoe;
    public float stepHeight = 0.18f;
    public float stepDepth = 0.29f;
    public float stepDuration = 0.8f;
    public float curveStrength = 1.0f;
    public float transitionStiffnessShoe = 10.0f;

    public Transform headTransform;
    public float transitionStiffnessHeadY = 12f;
    public float transitionStiffnessHeadZ = 12f;
    private float currentHeadHeight;
    private float currentZPosition;

    public Transform leftTracker; // 左足のトリガーオブジェクト
    public Transform rightTracker; // 右足のトリガーオブジェクト
    public float triggerHeight = 0.3f; // オブジェクトが超えるべき高さ

    private float initialHeightLeftTracker; // leftTrackerの初期高さ
    private float initialHeightRightTracker; // rightTrackerの初期高さ
    private bool canTriggerLeft = true; // 左足の遷移を許可するかどうか
    private bool canTriggerRight = true; // 右足の遷移を許可するかどうか

    private bool isStepping = false;
    private bool isRightShoeTurn = true;
    private bool isFirstStep = true;
    private float progress = 0.0f;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float t0;
    private bool isRemapping = false;
    private float omega = 1f;

    void Start()
    {
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;
        initialHeightLeftTracker = leftTracker.position.y;
        initialHeightRightTracker = rightTracker.position.y;
    }

    void Update()
    {
        // トラッカーの高さに応じて遷移を開始
        if (!isStepping && !isRemapping)
        {
            if (isRightShoeTurn && canTriggerRight && rightTracker.position.y >= initialHeightRightTracker + triggerHeight)
            {
                StartStep();
                StartHeadRemap();
                canTriggerRight = false; // 右足のトリガーを無効化
            }
            else if (!isRightShoeTurn && canTriggerLeft && leftTracker.position.y >= initialHeightLeftTracker + triggerHeight)
            {
                StartStep();
                StartHeadRemap();
                canTriggerLeft = false; // 左足のトリガーを無効化
            }
        }

        // トリガーリセット
        if (!isStepping)
        {
            if (leftTracker.position.y <= initialHeightLeftTracker + 0.2f)
            {
                canTriggerLeft = true;
            }
            if (rightTracker.position.y <= initialHeightRightTracker + 0.2f)
            {
                canTriggerRight = true;
            }
        }

        if (isStepping)
        {
            MoveShoe();
        }

        if (isRemapping)
        {
            RemapHeadHeight();
        }
    }

    void StartStep()
    {
        isStepping = true;
        progress = 0.0f;

        float heightMultiplier = isFirstStep ? 1.0f : 2.0f;
        float depthMultiplier = isFirstStep ? 1.0f : 2.0f;

        if (isRightShoeTurn)
        {
            startPosition = rightShoe.position;
            targetPosition = rightShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
        }
        else
        {
            startPosition = leftShoe.position;
            targetPosition = leftShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
        }
    }

    void MoveShoe()
    {
        progress += Time.deltaTime / stepDuration;
        progress = Mathf.Clamp01(progress);

        float sigmoidProgress = 1 / (1 + Mathf.Exp(-transitionStiffnessShoe * (progress - 0.5f)));

        Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, sigmoidProgress);

        currentPosition.y += Mathf.Sin(sigmoidProgress * Mathf.PI) * stepHeight * curveStrength;

        if (isRightShoeTurn)
        {
            rightShoe.position = currentPosition;
        }
        else
        {
            leftShoe.position = currentPosition;
        }

        if (progress >= 1.0f)
        {
            isStepping = false;
            isRightShoeTurn = !isRightShoeTurn;
            isFirstStep = false;
        }
    }

    void StartHeadRemap()
    {
        if (!isRemapping)
        {
            t0 = Time.time;
            isRemapping = true;
        }
    }

    void RemapHeadHeight()
    {
        float currentTime = Time.time - t0;
        float delta = stepDuration / (5f - 2f * omega);

        if (currentTime >= stepDuration)
        {
            isRemapping = false;
            currentHeadHeight = headTransform.position.y;
            currentZPosition = headTransform.position.z;
        }
        else
        {
            float newHeight = currentHeadHeight + omega * stepHeight * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadY * (currentTime - delta))));
            float newZPosition = currentZPosition + omega * stepDepth * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadZ * (currentTime - delta))));

            headTransform.position = new Vector3(headTransform.position.x, newHeight, newZPosition);
        }
    }
}
