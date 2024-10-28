using UnityEngine;

public class HeadMove : MonoBehaviour
{
    public Transform headTransform;  // HMDのトランスフォーム
    public float stairRise = 0.12f;  // 各階段ステップの上昇量
    public float forwardStep = 0.29f;  // 各ステップごとの前進距離
    public float transitionTime = 0.8f;  // 遷移時間 T
    public float transitionStiffnessY = 12f;  // 遷移の硬さ（高さ） a
    public float transitionStiffnessZ = 12f;  // 遷移の硬さ（前進） a
    private float currentHeadHeight;  // 現在の頭部の高さ（累積的な高さ）
    private float currentZPosition;  // 現在のZ方向位置（累積的な位置）
    private float t0;  // 足が上昇し始めた時刻
    private bool isRemapping = false;  // リマッピングが進行中かどうかを判断
    private float omega = 1f;  // 上りの場合は1、下りの場合は-1

    void Start()
    {
        // 初期の頭部の高さと前進位置を設定
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;
    }

    void Update()
    {
        // リマッピングが進行中でなければ新たなリマッピングを開始する
        if (!isRemapping && Input.GetKeyDown(KeyCode.Space))  
        {
            t0 = Time.time;  // 開始時間をリセット
            isRemapping = true;  // リマッピング開始
        }

        // リマッピング処理を進行中であれば呼び出す
        if (isRemapping)
        {
            RemapHeadHeight();
        }
    }

    void RemapHeadHeight()
    {
        float currentTime = Time.time - t0;
        float delta = transitionTime / (5f - 2f * omega);  // シフトパラメータの計算

        // 遷移時間が過ぎたらリマッピングを停止し、高さと前進位置を新たな基準とする
        if (currentTime >= transitionTime)
        {
            isRemapping = false;  // リマッピング終了
            currentHeadHeight = headTransform.position.y;  // 現在の高さを基準として設定
            currentZPosition = headTransform.position.z;  // 現在の前進位置を基準として設定
        }
        else
        {
            // シグモイド関数を使ったスムーズな頭部のリマッピング
            float newHeight = currentHeadHeight + omega * stairRise * (1 / (1 + Mathf.Exp(-transitionStiffnessY * (currentTime - delta))));
            float newZPosition = currentZPosition + omega * forwardStep * (1 / (1 + Mathf.Exp(-transitionStiffnessZ * (currentTime - delta))));

            // 頭部の位置を更新
            headTransform.position = new Vector3(headTransform.position.x, newHeight, newZPosition);
        }
    }
}