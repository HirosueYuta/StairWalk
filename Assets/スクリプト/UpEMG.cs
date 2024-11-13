using UnityEngine;

public class UpEMG : MonoBehaviour
{
    // 靴のトランスフォームと階段のステップに関するパラメータ
    public Transform rightShoe;  // 右靴のTransform
    public Transform leftShoe;   // 左靴のTransform
    public float stepHeight = 0.18f;  // 階段の高さ
    public float stepDepth = 0.29f;   // 階段の幅
    public float stepDuration = 0.8f;  // 各ステップにかける時間
    public float curveStrength = 1.0f;  // 曲線のカーブの強さを調整する係数
    public float transitionStiffnessShoe = 10.0f; // シグモイド関数の硬さ

    // HMDに関するパラメータ
    public Transform headTransform;  // HMDのトランスフォーム
    public float transitionStiffnessHeadY = 12f;  // 頭部リマッピングの硬さ（高さ）
    public float transitionStiffnessHeadZ = 12f;  // 頭部リマッピングの硬さ（前進）
    private float currentHeadHeight;  // 現在の頭部の高さ（累積的な高さ）
    private float currentZPosition;  // 現在のZ方向位置（累積的な位置）

    // EMGデータ受信用の参照と閾値
    public EMGDataReceiver emgDataReceiver;  // EMGデータ受信スクリプトの参照
    public float emgThreshold = 0.5f;  // ステップ開始のための筋電位閾値

    // 内部処理用のフラグや変数
    private bool isStepping = false; // 階段を登っている最中かどうか
    private bool isRightShoeTurn = true; // 現在のステップが右靴か左靴か
    private bool isFirstStep = true;  // 最初のステップかどうか
    private float progress = 0.0f;  // 移動の進行状況を管理
    private Vector3 startPosition;  // 靴の移動の開始位置
    private Vector3 targetPosition; // 靴の移動の目標位置
    private float t0;  // 足が上昇し始めた時刻
    private bool isRemapping = false;  // 頭部リマッピングが進行中かどうか
    private float omega = 1f;  // 上りの場合は1、下りの場合は-1

    void Start()
    {
        // 初期の頭部の高さと前進位置を設定
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;
    }

    void Update()
    {
        // 遷移中でなく、左右交互での筋電位入力が閾値を超えているかをチェック
        if (!isStepping && !isRemapping)
        {
            // 右足のターンかつ筋電位が閾値を超えた場合
            if (isRightShoeTurn && emgDataReceiver.emgValue1 >= emgThreshold)
            {
                StartStep();
                StartHeadRemap();
            }
            // 左足のターンかつ筋電位が閾値を超えた場合
            else if (!isRightShoeTurn && emgDataReceiver.emgValue2 >= emgThreshold)
            {
                StartStep();
                StartHeadRemap();
            }
        }

        // 靴のステップ処理
        if (isStepping)
        {
            MoveShoe();
        }

        // 頭部のリマッピング処理
        if (isRemapping)
        {
            RemapHeadHeight();
        }
    }

    // 新しい靴のステップを開始する関数
    void StartStep()
    {
        isStepping = true;   // ステップ進行中のフラグを立てる
        progress = 0.0f;     // ステップの進行度を初期化

        // 最初のステップのみ高さと幅を1段分、それ以降は2段分に設定
        float heightMultiplier = isFirstStep ? 1.0f : 2.0f;
        float depthMultiplier = isFirstStep ? 1.0f : 2.0f;

        // 右靴か左靴かによって開始位置と目標位置を設定
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

    // 靴のステップ移動処理
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
            isStepping = false;              // ステップ進行中フラグを解除
            isRightShoeTurn = !isRightShoeTurn; // 必ず左右交互に動作するように切り替え
            isFirstStep = false;             // 最初のステップフラグを解除
        }
    }

    // 頭部リマッピングの開始処理
    void StartHeadRemap()
    {
        if (!isRemapping)
        {
            t0 = Time.time;  // リマッピング開始時刻を記録
            isRemapping = true;  // リマッピング進行中フラグを設定
        }
    }

    // 頭部リマッピングの処理
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
