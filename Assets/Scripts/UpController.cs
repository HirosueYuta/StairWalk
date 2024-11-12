using UnityEngine;

public class UpController : MonoBehaviour
{
    public Transform rightShoe;  // 右靴のTransformを指定
    public Transform leftShoe;   // 左靴のTransformを指定
    public float stepHeight = 0.18f;  // 階段の高さ
    public float stepDepth = 0.29f;   // 階段の幅
    public float stepDuration = 0.8f;  // ステップ時間
    public float curveStrength = 1.0f;  // カーブの強さ
    public float transitionStiffnessShoe = 10.0f; // 靴移動の硬さ

    public Transform headTransform;  // HMDのTransformを指定
    public float transitionStiffnessHeadY = 12f;  // 頭部リマッピング硬さ（垂直）
    public float transitionStiffnessHeadZ = 12f;  // 頭部リマッピング硬さ（前方）
    private float currentHeadHeight;
    private float currentZPosition;

    private bool isStepping = false;
    private bool isRightShoeTurn = true; // 現在のステップが右靴か左靴かを制御
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
        // 各ボタンが押されたときにステップを開始
        if (!isStepping && !isRemapping)
        {
            if (isRightShoeTurn && Input.GetKeyDown(KeyCode.JoystickButton1))  // Aボタン（右）device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger)
            {
                StartStep();
                StartHeadRemap();
            }
            else if (!isRightShoeTurn && Input.GetKeyDown(KeyCode.JoystickButton2))  // Xボタン（左）
            {
                StartStep();
                StartHeadRemap();
            }
        }

        // ステップとリマッピングの進行
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
