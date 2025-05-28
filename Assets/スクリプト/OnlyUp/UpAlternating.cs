using UnityEngine;

public class UpAlternating : MonoBehaviour
{
    public Transform rightShoe;
    public Transform leftShoe;
    public float stepHeight = 0.18f;
    public float stepDepth = 0.29f;
    public float stepDuration = 0.8f;
    private bool isKeyBuffered = false; // キー入力をバッファするフラグ
    public float BufferTime = 0.7f; // キー入力をバッファするタイミング
    public float curveStrength = 1.0f;
    public float transitionStiffnessShoe = 10.0f;

    public Transform headTransform;
    public float transitionStiffnessHeadY = 12f;
    public float transitionStiffnessHeadZ = 12f;

    private float currentHeadHeight;
    private float currentZPosition;
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
    }

    void Update()
    {
        // ステップ中でなく、バッファにキーが保持されている場合は新しいステップを開始
        if (!isStepping && !isRemapping && isKeyBuffered)
        {
            isKeyBuffered = false; // バッファをクリア
            StartStep();
            StartHeadRemap();
        }
        
        // 通常の入力処理：ステップやリマッピングが進行していない場合のみ開始
        if (Input.GetKeyDown(KeyCode.Space) && !isStepping && !isRemapping)
        {
            StartStep();
            StartHeadRemap();
        }

        // 遷移中の入力バッファ処理
        if (isStepping && progress >= BufferTime && Input.GetKeyDown(KeyCode.Space))
        {
            isKeyBuffered = true; // 0.7秒以降の入力をバッファ
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
