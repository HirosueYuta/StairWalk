using UnityEngine;

public class UpAlternating : MonoBehaviour
{
    // 靴の位置および階段のステップに関する設定
    public Transform rightShoe;  // 右靴のTransformを指定
    public Transform leftShoe;   // 左靴のTransformを指定
    public float stepHeight = 0.18f;  // 階段の高さ（垂直方向の移動量）
    public float stepDepth = 0.29f;   // 階段の幅（前方への移動量）
    public float stepDuration = 0.8f;  // 各ステップにかかる時間（秒）
    public float curveStrength = 1.0f;  // ステップの際にカーブを描く強さ
    public float transitionStiffnessShoe = 10.0f; // 靴の移動に用いるシグモイド関数の硬さ

    // 頭部（HMD）の位置およびリマッピングに関する設定
    public Transform headTransform;  // HMDのTransformを指定
    public float transitionStiffnessHeadY = 12f;  // 頭部リマッピングの硬さ（垂直）
    public float transitionStiffnessHeadZ = 12f;  // 頭部リマッピングの硬さ（前方）
    private float currentHeadHeight;  // 頭部の現在の高さ（累積的に保持）
    private float currentZPosition;  // 頭部の現在のZ方向位置（累積的に保持）

    // 内部処理用のフラグと変数
    private bool isStepping = false; // 階段のステップが進行中であるかを判定
    private bool isRightShoeTurn = true; // 現在のステップが右靴のターンか左靴のターンかを判定
    private bool isFirstStep = true;  // 最初のステップかどうかを判定
    private float progress = 0.0f;  // ステップの進行状況（0～1の範囲）
    private Vector3 startPosition;  // 現在の靴の移動開始位置
    private Vector3 targetPosition; // 現在の靴の移動目標位置
    private float t0;  // リマッピング開始時刻
    private bool isRemapping = false;  // 頭部リマッピングが進行中かどうか
    private float omega = 1f;  // 上りの場合は1、下りの場合は-1

    void Start()
    {
        // 頭部の初期位置（高さおよびZ方向位置）を設定
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;
    }

    void Update()
    {
        // スペースキーが押された場合、新しいステップを開始
        // ただし、現在のステップおよびリマッピングが進行中でないことが条件
        if (Input.GetKeyDown(KeyCode.Space) && !isStepping && !isRemapping)
        {
            StartStep();       // 靴のステップ処理を開始
            StartHeadRemap();  // 頭部リマッピングを開始
        }

        // 靴のステップが進行中の場合、移動を行う
        if (isStepping)
        {
            MoveShoe();  // 靴の移動処理
        }

        // 頭部のリマッピングが進行中の場合、リマッピング処理を実行
        if (isRemapping)
        {
            RemapHeadHeight();  // 頭部リマッピング処理
        }
    }

    // 新しい靴のステップを開始する関数
    void StartStep()
    {
        isStepping = true;   // ステップ進行中のフラグを立てる
        progress = 0.0f;     // ステップの進行度を初期化

        // 最初のステップでは1段分、それ以降は2段分の移動距離を設定
        float heightMultiplier = isFirstStep ? 1.0f : 2.0f;
        float depthMultiplier = isFirstStep ? 1.0f : 2.0f;

        // 右靴か左靴かによって開始位置と目標位置を設定
        if (isRightShoeTurn)
        {
            startPosition = rightShoe.position;  // 現在の右靴の位置
            targetPosition = rightShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
        }
        else
        {
            startPosition = leftShoe.position;  // 現在の左靴の位置
            targetPosition = leftShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
        }
    }

    // 靴のステップ移動処理
    void MoveShoe()
    {
        // 時間に基づいて進行度を増加させ、0～1の範囲内に制限
        progress += Time.deltaTime / stepDuration;
        progress = Mathf.Clamp01(progress);

        // シグモイド関数で進行度を滑らかに変化させる
        float sigmoidProgress = 1 / (1 + Mathf.Exp(-transitionStiffnessShoe * (progress - 0.5f)));

        // 線形補間（Lerp）を使用して靴の位置を更新
        Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, sigmoidProgress);

        // 曲線を描くようにy軸の高さを調整
        currentPosition.y += Mathf.Sin(sigmoidProgress * Mathf.PI) * stepHeight * curveStrength;

        // 右靴か左靴かに応じて靴の位置を更新
        if (isRightShoeTurn)
        {
            rightShoe.position = currentPosition;
        }
        else
        {
            leftShoe.position = currentPosition;
        }

        // ステップが完了した場合
        if (progress >= 1.0f)
        {
            isStepping = false;             // ステップ進行中フラグを解除
            isRightShoeTurn = !isRightShoeTurn; // 次の靴に切り替え
            isFirstStep = false;            // 最初のステップフラグを解除
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
        // 現在の経過時間を計算
        float currentTime = Time.time - t0;
        float delta = stepDuration / (5f - 2f * omega);  // シフトパラメータの計算

        // リマッピングが終了する条件
        if (currentTime >= stepDuration)
        {
            isRemapping = false;              // リマッピング進行中フラグを解除
            currentHeadHeight = headTransform.position.y;  // 現在の高さを基準として設定
            currentZPosition = headTransform.position.z;   // 現在の前進位置を基準として設定
        }
        else
        {
            // シグモイド関数を使用して滑らかに頭部位置を更新
            float newHeight = currentHeadHeight + omega * stepHeight * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadY * (currentTime - delta))));
            float newZPosition = currentZPosition + omega * stepDepth * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadZ * (currentTime - delta))));

            // 頭部の位置を更新
            headTransform.position = new Vector3(headTransform.position.x, newHeight, newZPosition);
        }
    }
}
