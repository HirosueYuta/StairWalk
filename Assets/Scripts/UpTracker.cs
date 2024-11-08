using UnityEngine;

public class UpTracker : MonoBehaviour
{
    // 靴と頭部のTransformの参照
    public Transform rightShoe;          // 右靴のTransform
    public Transform leftShoe;           // 左靴のTransform
    public Transform headTransform;      // 頭部のTransform
    public Transform leftTracker;        // 左足のトラッカー
    public Transform rightTracker;       // 右足のトラッカー
    
    // ステップ動作に関連するパラメータ
    public float stepHeight = 0.18f;     // ステップの高さ
    public float stepDepth = 0.29f;      // ステップの奥行き
    public float stepDuration = 0.8f;    // ステップの継続時間
    public float curveStrength = 1.0f;   // ステップの曲線強度
    public float transitionStiffnessShoe = 10.0f; // 靴の遷移の滑らかさ
    
    // 頭部のリマッピングに関連するパラメータ
    public float transitionStiffnessHeadY = 12f;  // Y軸リマッピングの滑らかさ
    public float transitionStiffnessHeadZ = 12f;  // Z軸リマッピングの滑らかさ
    private float initialHeadHeight;
    private float currentHeadHeight;              // 現在の頭部高さ
    private float currentZPosition;               // 現在の頭部Z位置

    // トラッカーの高さ関連
    public float visualGain = 1.193f;            // 視覚的ゲイン
    private float initialHeightLeftTracker;      // 左トラッカーの初期高さ
    private float initialHeightRightTracker;     // 右トラッカーの初期高さ
    private float RelativeHeightRightTracker;
    private float RelativeHeightLeftTracker;
    private bool canTriggerLeft = true;          // 左足のトリガー許可
    private bool canTriggerRight = true;         // 右足のトリガー許可
    private bool isInitialHeightSet = false;     //トラッカーの初期高さ設定をしたかどうか

    // ステップ状態とフラグ
    private bool isStepping = false;             // ステップ中かどうか
    private bool isRightShoeTurn = true;         // 現在右靴の番かどうか
    private bool isFirstStep = true;             // 最初のステップかどうか
    private float progress = 0.0f;               // ステップの進行状況
    private Vector3 startPosition;               // ステップ開始位置
    private Vector3 targetPosition;              // ステップ目標位置
    private float t0;                            // リマッピング開始時間
    private bool isRemapping = false;            // リマッピング中かどうか
    private float omega = 1f;                    // リマッピングの調整用係数

    void Start()
    {
        // 頭部の初期位置（高さとZ軸）を記録
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;
    }

    void Update()
    {
        // 初期高さが未設定の場合のみ初期高さを取得
        if (!isInitialHeightSet || initialHeightLeftTracker == 0 || initialHeightRightTracker == 0)
        {
            initialHeadHeight = headTransform.position.y; //＊＊頭の初期位置は必ず０である
            initialHeightLeftTracker = leftTracker.position.y;
            initialHeightRightTracker = rightTracker.position.y;
            isInitialHeightSet = true; // 初期高さの取得を完了
            //print("initialHeight:(" + initialHeadHeight+ "," + initialHeightLeftTracker + "," + initialHeightRightTracker + ")");
        }

        
        // ステップまたはリマッピングが進行中でない場合のみ,トラッカーの初期位置を設定した後の場合のみ遷移のチェック
        if (!isStepping && !isRemapping && isInitialHeightSet)
        {
            // トラッカーの高さを相対的に計算
            RelativeHeightRightTracker = rightTracker.position.y - (currentHeadHeight - initialHeadHeight);
            RelativeHeightLeftTracker = leftTracker.position.y - (currentHeadHeight - initialHeadHeight);
            //print("Relative Height Tracker: (" + RelativeHeightLeftTracker + "," + RelativeHeightRightTracker + ")");
            //print("initialHeightRightTracker + stepHeight / visualGain:"+(initialHeightRightTracker+stepHeight/visualGain));
        
            // トラッカーの高さがしきい値を超えた場合、ステップと頭部リマッピングを開始
            if (isRightShoeTurn && canTriggerRight && RelativeHeightRightTracker >= (initialHeightRightTracker + stepHeight / visualGain))
            {
                StartStep();
                StartHeadRemap();
                canTriggerRight = false; // 右足トリガーを無効化
                //print("canTriggerRight:"+canTriggerRight);
            }
            else if (!isRightShoeTurn && canTriggerLeft && RelativeHeightLeftTracker >= (initialHeightLeftTracker + stepHeight / visualGain))
            {
                StartStep();
                StartHeadRemap();
                canTriggerLeft = false; // 左足トリガーを無効化
                //print("canTriggerLeft:"+canTriggerLeft);
            }
        }

        // トリガーリセットの条件を満たす場合、再び遷移を許可
        if (!isStepping)
        {
            if (RelativeHeightLeftTracker <= initialHeightLeftTracker + 0.2f)
            {
                canTriggerLeft = true;
                //print("canTriggerLeft:"+canTriggerLeft);
            }
            if (RelativeHeightRightTracker <= initialHeightRightTracker + 0.2f)
            {
                canTriggerRight = true;
                //print("canTriggerRight:"+canTriggerRight);
            }
        }

        // ステップが進行中の場合、靴の位置を更新
        if (isStepping)
        {
            MoveShoe();
        }

        // リマッピングが進行中の場合、頭部位置のリマッピングを実行
        if (isRemapping)
        {
            RemapHeadHeight();
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
        // 進行度を更新し、0～1の範囲に制限
        progress += Time.deltaTime / stepDuration;
        progress = Mathf.Clamp01(progress);

        // シグモイド関数で滑らかな進行度を計算
        float sigmoidProgress = 1 / (1 + Mathf.Exp(-transitionStiffnessShoe * (progress - 0.5f)));

        // 線形補間で靴の位置を更新し、y軸の高さを調整
        Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, sigmoidProgress);
        currentPosition.y += Mathf.Sin(sigmoidProgress * Mathf.PI) * stepHeight * curveStrength;

        // 右靴または左靴の位置を更新
        if (isRightShoeTurn)
        {
            rightShoe.position = currentPosition;
        }
        else
        {
            leftShoe.position = currentPosition;
        }

        // ステップ完了時の処理
        if (progress >= 1.0f)
        {
            isStepping = false;            // ステップ終了
            isRightShoeTurn = !isRightShoeTurn; // 次の靴に切り替え
            isFirstStep = false;           // 最初のステップフラグ解除
        }
    }

    // 頭部リマッピングの開始処理
    void StartHeadRemap()
    {
        if (!isRemapping)
        {
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
}
