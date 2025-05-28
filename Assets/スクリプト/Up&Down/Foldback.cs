using UnityEngine;

public class Foldback : MonoBehaviour
{
    [Header("頭オブジェクト")] 
    [SerializeField] Transform headTransform;

    [Header("検知回数ごとの目標位置リスト")]
    [SerializeField] Vector3[] targetPositions;

    [Header("検知回数ごとの目標回転リスト（Euler角度）")]
    [SerializeField] Vector3[] targetEulerAngles;
    
    void OnEnable()
    {
        // イベント登録
        ZoneDetector.OnZoneEntered += HandleZoneEntered;
    }

    void OnDisable()
    {
        // イベント解除
        ZoneDetector.OnZoneEntered -= HandleZoneEntered;
    }

    // ③ イベントハンドラ
    void HandleZoneEntered(int count)
    {
        // 配列の範囲を超えないように丸め込む（必要に応じて）
        int index = Mathf.Clamp(count - 1, 0, targetPositions.Length - 1);

        // 位置設定
        Vector3 newPos = targetPositions[index];

        // 回転設定：Inspector に入力した Euler 角度を Quaternion に変換
        Quaternion newRot = Quaternion.Euler(targetEulerAngles[index]);

        // 位置と回転を同時に更新
        headTransform.SetPositionAndRotation(newPos, newRot);

        Debug.Log($"Foldback: {count} 回目 → Pos: {newPos}, Rot: {targetEulerAngles[index]}");
    }
}
