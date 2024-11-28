using UnityEngine;
public class Up2Alternating : MonoBehaviour
{
    public Transform rightShoe;          // 右靴のTransform
    public Transform leftShoe;           // 左靴のTransform
    public Transform headTransform;      // HMD（頭部）のTransform
    public Transform headCamera;         //HMDの位置

    public float stepHeight = 0.18f;     // ステップの高さ
    public float stepDepth = 0.29f;      // ステップの奥行き
    public float stepDuration = 0.8f;    // ステップの継続時間
    public float curveStrength = 1.0f;   // ステップの曲線強度
    public float transitionStiffnessShoe = 10.0f; // 靴移動の滑らかさ

    public float transitionStiffnessHeadY = 12f;  // 頭部Y軸リマッピングの滑らかさ
    public float transitionStiffnessHeadZ = 12f;  // 頭部Z軸リマッピングの滑らかさ
    private float currentHeadHeight;             // 現在の頭部高さ
    private float currentZPosition;              // 現在の頭部Z位置

    private bool grapgripLeftHand;
    private bool grapgripRightHand;

    private bool isStepping = false;    // ステップ中かどうか
    public bool isRightFootNext = true;
    public bool isRightShoeTurn = false;
    public bool isLeftShoeTurn = false;
    private bool isFirstStep = true;
    private float progress = 0.0f;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float t0;
    private bool isRemapping = false;
    private float omega = 1f;

    // 入力バッファ
    private bool bufferedLeftInput = false;
    private bool bufferedRightInput = false;

    //初期位置合わせ
    private bool isInitialHeadPositiontSet = false;
    private float initialHeadPositionX = 0f;
    private float initialHeadPositionZ = 0f;

    void Start()
    {
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;

        isLeftShoeTurn = false;
        isRightShoeTurn = false;
        isRightFootNext = true;
    }

    void Update()
    {
        //初期位置調整
        if (!isInitialHeadPositiontSet){
            SetInitialHeadPosition();
        }

        // キーボード入力を取得
        grapgripLeftHand = Input.GetKeyDown(KeyCode.A);
        grapgripRightHand = Input.GetKeyDown(KeyCode.D);

        //バッファ入力がないとき
        if (!isStepping)
        {
            // 通常の入力を処理
            if (grapgripRightHand && isRightFootNext)
            {
                isRightShoeTurn = true;
                StartStep();
                StartHeadRemap();
            }
            else if (grapgripLeftHand && !isRightFootNext)
            {
                isLeftShoeTurn = true;
                StartStep();
                StartHeadRemap();
            }
        }
        // ステップ中の入力をバッファに保存
        else
        {
            if (grapgripRightHand && !isRightFootNext)
            {
                bufferedRightInput = true;
            }
            else if (grapgripLeftHand && isRightFootNext)
            {
                bufferedLeftInput = true;
            }
        }

        // ステップ処理
        if (isStepping)
        {
            MoveShoe();
        }

        // 頭部リマッピング
        if (isRemapping)
        {
            RemapHeadHeight();
        }
    }

    void SetInitialHeadPosition(){
    //頭の初期位置調整
    initialHeadPositionX = headCamera.position.x;
    initialHeadPositionZ = headCamera.position.z; 

    if (initialHeadPositionX != 0 || initialHeadPositionZ != 0){
        headTransform.position = new Vector3(headTransform .position.x - headCamera.position.x, headTransform.position.y, headTransform.position.z-headCamera.position.z);
        Debug.Log(initialHeadPositionX+","+initialHeadPositionZ);
        isInitialHeadPositiontSet = true; // 初期高さの取得を完了
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
        else if (isLeftShoeTurn)
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
        else if (isLeftShoeTurn)
        {
            leftShoe.position = currentPosition;
        }

        if (progress >= 1.0f)
        {
            isStepping = false;
            isRightFootNext = !isRightFootNext;

            // ステップ完了後にターンをリセット
            if (isRightShoeTurn)
            {
                isRightShoeTurn = false;
            }
            else if (isLeftShoeTurn)
            {
                isLeftShoeTurn = false;
            }

            isFirstStep = false;

            // バッファ処理
            ProcessBufferedInput();
        }
    }

    void ProcessBufferedInput()
    {
        if (bufferedRightInput && isRightFootNext)
        {
            bufferedRightInput = false;
            isRightShoeTurn = true;
            
            // 頭部リマッピングを再リセットして再開
            ResetHeadRemap();

            StartStep();
            StartHeadRemap();
        }
        else if (bufferedLeftInput && !isRightFootNext)
        {
            bufferedLeftInput = false;
            isLeftShoeTurn = true;

            // 頭部リマッピングを再リセットして再開
            ResetHeadRemap();

            StartStep();
            StartHeadRemap();
        }
    }

    void StartHeadRemap()
    {
        if (!isRemapping)
        {
            // 頭部の現在位置をリマッピングの基準として保存
            currentHeadHeight = headTransform.position.y;
            currentZPosition = headTransform.position.z;
            
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
    void ResetHeadRemap()
    {
        isRemapping = false; // リマッピング状態をリセット
        t0 = Time.time;      // 現在の時刻を再設定
    }
}