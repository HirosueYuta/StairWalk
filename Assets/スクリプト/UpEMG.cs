using UnityEngine;

public class UpEMG : MonoBehaviour
{
    // 靴のTransformと階段動作に関連するパラメータ
    public Transform rightShoe;  // 右靴のTransform
    public Transform leftShoe;   // 左靴のTransform
    public float stepHeight = 0.18f;  // ステップの高さ
    public float stepDepth = 0.29f;   // ステップの奥行き
    public float stepDuration = 0.8f; // ステップにかかる時間
    public float curveStrength = 1.0f;  // 靴の移動中の曲線強度
    public float transitionStiffnessShoe = 10.0f; // 靴移動の滑らかさ

    // 頭部（HMD）のTransformとリマッピングに関連するパラメータ
    public Transform headTransform;  // 頭部のTransform
    public float transitionStiffnessHeadY = 12f;  // 頭部の高さリマッピングの滑らかさ
    public float transitionStiffnessHeadZ = 12f;  // 頭部の奥行きリマッピングの滑らかさ
    private float currentHeadHeight;  // 現在の頭部高さ
    private float currentZPosition;   // 現在の頭部のZ軸位置

    // 筋電位（EMG）データ受信用
    public EMGDataReceiver emgDataReceiver;  // EMGデータ受信スクリプト
    public EMGDataMaxTracker emgDataMaxTracker; // EMG最大値トラッキングスクリプト
    public float emgThreshold = 50f; // ステップを開始するための筋電位のしきい値
    [Range(0f, 1f)] public float thresholdRatio = 0.6f; // 最大値に対するしきい値の割合

    // ステップ処理と状態管理
    private bool isStepping = false;  // ステップが進行中かどうか
    private bool isRightShoeTurn = true; // 現在右靴の番かどうか
    private bool isFirstStep = true;  // 最初のステップかどうか
    private float progress = 0.0f;  // ステップの進行状況
    private Vector3 startPosition;  // ステップ開始位置
    private Vector3 targetPosition; // ステップ目標位置

    // 頭部リマッピング管理
    private float t0;  // リマッピング開始時刻
    private bool isRemapping = false; // リマッピングが進行中かどうか
    private float omega = 1f;  // リマッピングの方向（上りは1、下りは-1）

    // 入力バッファ関連
    private bool isInputBuffered = false;        // 入力がバッファされているか
    public float BufferTime = 0f;              // 入力をバッファするタイミング

    void Start()
    {
        // 頭部（HMD）の初期高さとZ位置を記録
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;
    }

    void Update()
    {
        // 最大値の6割をしきい値に設定
        if (emgDataMaxTracker != null)
        {
            emgThreshold = Mathf.Min(emgDataMaxTracker.maxEmgValue1, emgDataMaxTracker.maxEmgValue2) * thresholdRatio;
        }
        // ステップ中の場合、靴の位置を更新
        if (isStepping)
        {
            MoveShoe();

            // ステップ中（進行度がBufferTime以降）での入力をバッファ
            if (progress >= BufferTime)
            {
                if (isRightShoeTurn && emgDataReceiver.emgValue1 >= emgThreshold)
                {
                    isInputBuffered = true; // 右足の入力をバッファ
                }
                else if (!isRightShoeTurn && emgDataReceiver.emgValue2 >= emgThreshold)
                {
                    isInputBuffered = true; // 左足の入力をバッファ
                }
            }
        }

        // ステップ終了後にバッファされた入力を処理
        if (!isStepping && !isRemapping && isInputBuffered)
        {
            isInputBuffered = false; // バッファをクリア
            StartStep();             // 次のステップを開始
            StartHeadRemap();        // 頭部リマッピングを開始
        }

        // 通常のEMG入力処理（ステップやリマッピングが進行中でない場合のみ）
        if (!isStepping && !isRemapping)
        {
            if (isRightShoeTurn && emgDataReceiver.emgValue1 >= emgThreshold)
            {
                StartStep();       // 右足のステップを開始
                StartHeadRemap();  // 頭部リマッピングを開始
            }
            else if (!isRightShoeTurn && emgDataReceiver.emgValue2 >= emgThreshold)
            {
                StartStep();       // 左足のステップを開始
                StartHeadRemap();  // 頭部リマッピングを開始
            }
        }

        // リマッピングが進行中の場合、頭部の位置を更新
        if (isRemapping)
        {
            RemapHeadHeight();
        }
    }

    // 新しい靴のステップを開始する
    void StartStep()
    {
        isStepping = true;  // ステップが進行中であることを記録
        progress = 0.0f;    // ステップ進行状況をリセット

        // ステップの目標位置を設定（右足か左足かで分岐）
        float heightMultiplier = isFirstStep ? 1.0f : 2.0f;
        float depthMultiplier = isFirstStep ? 1.0f : 2.0f;

        if (isRightShoeTurn)
        {
            startPosition = rightShoe.position; // 右靴の現在位置を記録
            targetPosition = rightShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
        }
        else
        {
            startPosition = leftShoe.position; // 左靴の現在位置を記録
            targetPosition = leftShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
        }
    }

    // 靴のステップ移動処理
    void MoveShoe()
    {
        // 進行状況を時間に基づいて更新
        progress += Time.deltaTime / stepDuration;
        progress = Mathf.Clamp01(progress); // 進行状況を0～1に制限

        // シグモイド関数で滑らかな進行度を計算
        float sigmoidProgress = 1 / (1 + Mathf.Exp(-transitionStiffnessShoe * (progress - 0.5f)));

        // 線形補間で靴の位置を計算し、カーブを加える
        Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, sigmoidProgress);
        currentPosition.y += Mathf.Sin(sigmoidProgress * Mathf.PI) * stepHeight * curveStrength;

        if (isRightShoeTurn)
        {
            rightShoe.position = currentPosition; // 右靴の位置を更新
        }
        else
        {
            leftShoe.position = currentPosition; // 左靴の位置を更新
        }

        // ステップが完了した場合の処理
        if (progress >= 1.0f)
        {
            isStepping = false;            // ステップ進行フラグを解除
            isRightShoeTurn = !isRightShoeTurn; // 次のステップは反対の靴に切り替え
            isFirstStep = false;           // 最初のステップフラグを解除
        }
    }

    // 頭部リマッピングを開始する
    void StartHeadRemap()
    {
        if (!isRemapping)
        {
            t0 = Time.time;  // リマッピング開始時刻を記録
            isRemapping = true; // リマッピングフラグを設定
        }
    }

    // 頭部リマッピング処理
    void RemapHeadHeight()
    {
        float currentTime = Time.time - t0; // リマッピング開始からの経過時間を計算
        float delta = stepDuration / (5f - 2f * omega); // リマッピングタイミングを調整

        // リマッピング完了条件
        if (currentTime >= stepDuration)
        {
            isRemapping = false; // リマッピングフラグを解除
            currentHeadHeight = headTransform.position.y; // 現在の高さを記録
            currentZPosition = headTransform.position.z;  // 現在のZ位置を記録
        }
        else
        {
            // シグモイド関数で滑らかな高さとZ位置を計算
            float newHeight = currentHeadHeight + omega * stepHeight * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadY * (currentTime - delta))));
            float newZPosition = currentZPosition + omega * stepDepth * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadZ * (currentTime - delta))));

            headTransform.position = new Vector3(headTransform.position.x, newHeight, newZPosition); // 頭部位置を更新
        }
    }
}
