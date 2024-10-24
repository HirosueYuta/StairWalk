using UnityEngine;

public class HeadMove : MonoBehaviour
{
    public Transform headTransform;  // HMDのトランスフォーム
    public float stairRise = 0.12f;  // 各階段ステップの上昇量
    public float transitionTime = 0.8f;  // 遷移時間 T
    public float transitionStiffness = 12f;  // 遷移の硬さ a
    private float initialHeadHeight;
    private bool isAscending;  // 昇りか下りかのフラグ
    private float t0;  // 足が上昇し始めた時刻
    private float omega = 1f;  // 上りの場合は1、下りの場合は-1

    void Start()
    {
        // 初期の頭部の高さを取得
        initialHeadHeight = headTransform.position.y;
    }

    void Update()
    {
        // 足が上昇し始めた時刻を設定（エンターキーでトリガー）
        if (Input.GetKeyDown(KeyCode.Space))  // エンターキーで階段を上る動作を開始
        {
            t0 = Time.time;
            isAscending = true;
        }

        // 頭部のリマッピング処理
        RemapHeadHeight();
    }

    void RemapHeadHeight()
    {
        float currentTime = Time.time - t0;
        float realHeadHeight = headTransform.position.y;

        // シフトパラメータ δ を計算 (T = transitionTime, ω = omega)
        float delta = transitionTime / (5f - 2f * omega);

        // 遷移時間経過後は通常の高さを適用
        if (currentTime >= transitionTime)
        {
            headTransform.position = new Vector3(headTransform.position.x, realHeadHeight, headTransform.position.z);
        }
        else
        {
            // シグモイド関数を使ったスムーズな頭部のリマッピング
            float newHeight = initialHeadHeight + stairRise * (1 / (1 + Mathf.Exp(-transitionStiffness * (currentTime - delta))));
            headTransform.position = new Vector3(headTransform.position.x, newHeight, headTransform.position.z);
        }
    }
}
