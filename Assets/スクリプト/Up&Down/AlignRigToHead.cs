using UnityEngine;

public class AlignRigToHead : MonoBehaviour
{
       [Header("参照する Transform")]
    [Tooltip("頭として扱うオブジェクト")]
    public Transform headTransform;

    [Tooltip("このスクリプトをアタッチした CameraRig")]
    public Transform rigTransform;         // 通常は (transform)

    [Tooltip("Main Camera (HMD) の Transform")]
    public Transform cameraTransform;

    [Header("動作トリガー")]
    [Tooltip("チェックを入れると一度だけ Align を実行します")]
    public bool doAlign = false;

    void Reset()
    {
        // Reset 時に rigTransform を自分自身に設定
        rigTransform = transform;
    }

    void Update()
    {
        if (doAlign)
        {
            AlignOnce();
            doAlign = false;
        }
    }

    void AlignOnce()
    {
        if (headTransform == null || rigTransform == null || cameraTransform == null)
        {
            Debug.LogWarning("AlignRig: いずれかの Transform が設定されていません。");
            return;
        }

        // 1. ワールド座標を取得
        Vector3 headPos   = headTransform.position;
        Vector3 rigPos    = rigTransform.position;
        Vector3 cameraPos = cameraTransform.position;

        // 2. 頭とカメラの差分を計算
        Vector3 headToCam = headPos - cameraPos;

        // 3. CameraRig を差分だけ移動
        rigTransform.position += headToCam;

        Debug.Log($"AlignRig: rig moved by {headToCam:F3}  (head:{headPos:F3}, cam:{cameraPos:F3})");
    }
}
