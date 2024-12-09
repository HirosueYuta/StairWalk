using UnityEngine;

public class UpTracker : MonoBehaviour
{
    // 靴と頭部のTransformの参照
    public Transform rightShoe;          // 右靴のTransform
    public Transform leftShoe;           // 左靴のTransform
    public Transform headTransform;      // 頭部のTransform
    public Transform leftTracker;        // 左足のトラッカー
    public Transform rightTracker;       // 右足のトラッカー
    public Transform headCamera;         //HMDの位置
    
    // ステップ動作に関連するパラメータ
    private float stepHeight = 0.17995f;     // ステップの高さ
    private float stepDepth = 0.29f;      // ステップの奥行き
    public float stepDuration = 0.8f;    // ステップの継続時間
    private float curveStrength = 1.0f;   // ステップの曲線強度
    private float transitionStiffnessShoe = 10.0f; // 靴の遷移の滑らかさ
    
    // 頭部のリマッピングに関連するパラメータ
    private float transitionStiffnessHeadY = 12f;  // Y軸リマッピングの滑らかさ
    private float transitionStiffnessHeadZ = 12f;  // Z軸リマッピングの滑らかさ
    private float initialHeadHeight;
    private float currentHeadHeight;              // 現在の頭部高さ
    private float currentZPosition;               // 現在の頭部Z位置

    // トラッカーの高さ関連
    //public float visualGain = 1.193f;            // 視覚的ゲイン
    private float initialHeightLeftTracker;      // 左トラッカーの初期高さ
    private float initialHeightRightTracker;     // 右トラッカーの初期高さ
    public float RelativeHeightRightTracker;    //右トラッカーの相対高さ
    public float RelativeHeightLeftTracker;     //左トラッカの相対高さ
    private float previousRelativeHeightRightTracker;    //前フレームの右トラッカーの相対高さ
    private float previousRelativeHeightLeftTracker;     //左トラッカの相対高さ
    public bool isLeftFootUp = false;// 左足が上方向に移動しているかどうかを示すフラグ
    public bool isRightFootUp = false;// 右足が上方向に移動しているかどうかを示すフラグ

     // しきい値（ノイズ除去用）
    private float upwardThreshold = 0.005f; // この値以上の高さ変化があれば移動とみなす
    public bool canTriggerLeft = true;          // 左足のトリガー許可
    public bool canTriggerRight = true;         // 右足のトリガー許可
    private bool isInitialHeightSet = false;     //トラッカーの初期高さ設定をしたかどうか

    // ステップ状態とフラグ
    private bool isStepping = false;             // ステップ中かどうか

    public bool isRightFootNext = true;
    public bool isRightShoeTurn = false;
    public bool isLeftShoeTurn = false;   
    private bool isFirstStep = true;             // 最初のステップかどうか
    private float progress = 0.0f;               // ステップの進行状況
    private Vector3 startPosition;               // ステップ開始位置
    private Vector3 targetPosition;              // ステップ目標位置
    private float t0;                            // リマッピング開始時間
    private bool isRemapping = false;            // リマッピング中かどうか
    private float omega = 1f;                    // リマッピングの調整用係数

    // 入力バッファ
    private bool bufferedLeftInput = false;
    private bool bufferedRightInput = false;

    //初期位置合わせ
    private bool isInitialHeadPositiontSet = false;
    private float initialHeadPositionX = 0f;
    private float initialHeadPositionZ = 0f;

    void Start()
    {
        // 頭部の初期位置（高さとZ軸）を記録
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

        // 初期高さが未設定の場合のみ初期高さを取得
        if (!isInitialHeightSet || initialHeightLeftTracker == 0 || initialHeightRightTracker == 0)
        {
            SetInitialHeights();
        }

        //初期高さの設定が終わった後
        if (isInitialHeightSet)
        {
            //トラッカの相対的高さを取得
            UpdateRelativeHeights();

            //足が上がったか判定
            IsFootTriger();

            //バッファ入力がないとき
            if (!isStepping){
                //通常の入力を処理
                ProcessStepInput();
            }
            // ステップ中の入力をバッファに保存
            else
            {
                StoreBufferedInput();
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

            //最後に現在の高さを次フレーム用に保存
            previousRelativeHeightLeftTracker = RelativeHeightLeftTracker;
            previousRelativeHeightRightTracker = RelativeHeightRightTracker;
        }
    }

    void SetInitialHeadPosition(){
    //頭の初期位置調整
    initialHeadPositionX = headCamera.position.x;
    initialHeadPositionZ = headCamera.position.z; 

    if (!isInitialHeadPositiontSet && (initialHeadPositionX != 0 || initialHeadPositionZ != 0)){
        headTransform.position = new Vector3(headTransform .position.x - headCamera.position.x, headTransform.position.y, headTransform.position.z-headCamera.position.z);
        Debug.Log(initialHeadPositionX+","+initialHeadPositionZ);
        isInitialHeadPositiontSet = true; // 初期高さの取得を完了
        }
    }
    
    //頭の初期位置を設定
    void SetInitialHeights(){
        //頭の初期位置
        initialHeadHeight = headTransform.position.y; //＊＊頭の初期位置は必ず０である
        //各足のトラッカの初期位置
        initialHeightLeftTracker = leftTracker.position.y;
        initialHeightRightTracker = rightTracker.position.y;
        //初期化として前フレームの高さを現在の高さで設定
        previousRelativeHeightLeftTracker = leftTracker.position.y;
        previousRelativeHeightRightTracker = rightTracker.position.y;

        isInitialHeightSet = true; // 初期高さの取得を完了
        //print("initialHeight:(" + initialHeadHeight+ "," + initialHeightLeftTracker + "," + initialHeightRightTracker + ")");
    }

    void UpdateRelativeHeights(){
        // トラッカーの高さを相対的に計算
        RelativeHeightLeftTracker = leftTracker.position.y - (currentHeadHeight - initialHeadHeight);
        RelativeHeightRightTracker = rightTracker.position.y - (currentHeadHeight - initialHeadHeight);
        //Debug.Log("Relative Height Tracker: (" + RelativeHeightLeftTracker + "," + RelativeHeightRightTracker + ")");
        //Debug.Log("initialHeightRightTracker + stepHeight / visualGain:"+(initialHeightRightTracker+stepHeight/visualGain));

    }

    void IsFootTriger(){    
        // 足が初期値 + 10cm 以内にいるかどうか
        canTriggerLeft = RelativeHeightLeftTracker <= initialHeightLeftTracker + 0.1f;
        canTriggerRight = RelativeHeightRightTracker <= initialHeightRightTracker + 0.1f;
        //上方向に動いているかを判定
        isLeftFootUp = (RelativeHeightLeftTracker - previousRelativeHeightLeftTracker) > upwardThreshold;
        isRightFootUp = (RelativeHeightRightTracker - previousRelativeHeightRightTracker) > upwardThreshold;
    }

    void ProcessStepInput(){
        //通常の入力を処理
        if (isLeftFootUp && canTriggerLeft && !isRightFootNext)
        {
            isLeftShoeTurn = true;
            isLeftFootUp = false;
            StartStep();
            StartHeadRemap();
        }
        else if (isRightFootUp && canTriggerRight && isRightFootNext)
        {
            isRightShoeTurn = true;
            isRightFootUp = false;
            StartStep();
            StartHeadRemap();
        }
    }

    void StoreBufferedInput()
    {
        if (isLeftFootUp && canTriggerLeft && isRightFootNext)
        {
            bufferedLeftInput = true;
        }
        else if (isRightFootUp && canTriggerRight && !isRightFootNext)
        {
            bufferedRightInput = true;
        }
    }

    // 新しいステップの開始処理
    void StartStep()
    {
        isStepping = true;  // ステップ開始
        progress = 0.0f;    // 進行度の初期化

        // 最初のステップは1段、それ以降は2段ずつ移動
        float heightMultiplier = isFirstStep ? 1.0f : 2.0f;
        float depthMultiplier = isFirstStep ? 1.0f : 2.0f;

        // ステップの開始位置と目標位置を設定
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

    // 靴の移動処理
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

    // 頭部リマッピングの開始処理
    void StartHeadRemap()
    {
        if (!isRemapping)
        {
            // 頭部の現在位置をリマッピングの基準として保存
            currentHeadHeight = headTransform.position.y;
            currentZPosition = headTransform.position.z;

            t0 = Time.time;       // リマッピング開始時刻を記録
            isRemapping = true;    // リマッピングフラグを設定
        }
    }

    // 頭部のリマッピング処理
    void RemapHeadHeight()
    {
        float currentTime = Time.time - t0; // 経過時間
        float delta = stepDuration / (5f - 2f * omega); // リマッピングのタイミング調整

        // リマッピング完了の判定
        if (currentTime >= stepDuration)
        {
            isRemapping = false;
            currentHeadHeight = headTransform.position.y;
            currentZPosition = headTransform.position.z;
        }
        else
        {
            // シグモイド関数で新しい高さとZ位置を計算
            float newHeight = currentHeadHeight + omega * stepHeight * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadY * (currentTime - delta))));
            float newZPosition = currentZPosition + omega * stepDepth * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadZ * (currentTime - delta))));


            // 頭部の位置を更新
            headTransform.position = new Vector3(headTransform.position.x, newHeight, newZPosition);
        }
    }
    void ResetHeadRemap()
    {
        isRemapping = false; // リマッピング状態をリセット
        t0 = Time.time;      // 現在の時刻を再設定
    }
}
