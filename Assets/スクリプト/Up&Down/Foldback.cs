using UnityEngine; 

public class Foldback : MonoBehaviour
{
    [Header("頭オブジェクト")] 
    [SerializeField] Transform headTransform;

    [Header("検知回数ごとの目標位置リスト")]
    [SerializeField] Vector3[] targetPositions;

    [Header("検知回数ごとの目標回転リスト(Euler角度)")]
    [SerializeField] Vector3[] targetEulerAngles;

    [Header("HeadMoveFoldback 参照")]
    [SerializeField] HeadMoveFoldback headMover;

    // ① 補間中に検知が来たときに、あとで実行するための保留変数
    private int pendingCount;
    private bool hasPending = false;

    void OnEnable()
    {
        ZoneDetector.OnZoneEntered += HandleZoneEntered;
    }

    void OnDisable()
    {
        ZoneDetector.OnZoneEntered -= HandleZoneEntered;
    }

    void Update()
    {
        // ② 保留中かつ補間が終わっていたら、保留処理を実行
        if (hasPending && !headMover.IsRemapping)
        {
            bool currentFold = headMover.IsFolded;  // プロパティ方式で取得
            headMover.IsFolded = !currentFold;      // プロパティ方式で反転
            
            ExecuteFoldback(pendingCount);
            hasPending = false;

        }
    }

    void HandleZoneEntered(int count)
    {
        // ③ いまステップ補間中なら、保留して return
        if (headMover != null && headMover.IsRemapping)
        {
            //Debug.Log($"[Foldback] count={count} 検知だけどステップ補間中のため保留します");
            pendingCount = count;
            hasPending = true;
            return;
        }

        // ④ 補間中でなければ、すぐに折り返し処理
        ExecuteFoldback(count);
    }

    // ⑤ 実際のワープ（位置・回転）を行う共通メソッド
    private void ExecuteFoldback(int count)
    {
        int index = Mathf.Clamp(count - 1, 0, targetPositions.Length - 1);
        Vector3 newPos = targetPositions[index];
        Quaternion newRot = Quaternion.Euler(targetEulerAngles[index]);

        headTransform.SetPositionAndRotation(newPos, newRot);
        Debug.Log($"[Foldback] ワープ実行 → count={count}, Pos={newPos}, Rot={targetEulerAngles[index]}");
    }
}
