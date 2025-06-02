using UnityEngine;
using System;  // デリゲート用

public class ZoneDetector : MonoBehaviour
{
    [SerializeField] string targetTag = "Player";

    // ① イベントの定義（引数は「何回目の検知か」を int で渡す）
    public static event Action<int> OnZoneEntered;

    // ① static に変更：クラスで共通のカウンタ
    private static int detectCount = 0;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            detectCount++;
            Debug.Log($"{other.name} が領域に入りました（{detectCount} 回目）");
            
            // ② イベント発火
            OnZoneEntered?.Invoke(detectCount);
        }
    }


}
