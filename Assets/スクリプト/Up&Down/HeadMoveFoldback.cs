using UnityEngine;
using System;  // Action を使うために追加


// ファイル名：HeadMove.cs
public class HeadMoveFoldback : MonoBehaviour
{
    [Header("参照設定")]
    public Transform headTransform;

    [Header("ステップパラメータ")]
    private float stepHeight = 0.18f;
    private float stepDepth  = 0.29f;
    public float stepDuration = 0.8f;
    private float transitionStiffnessHeadY = 12f;
    private float transitionStiffnessHeadZ = 12f;

    public bool  isFolded     = false;   // ← 進行方向フラグ
    [SerializeField] public bool isRemapping = false;  // ステップ補間中フラグ
    private float t0;
    private float currentHeadHeight;
    private float currentZPosition;
    public float omega = 1f;

    // ここで「他のクラスから讀める」プロパティを追加
    public bool IsRemapping
    {
        get { return isRemapping; }
    }

    public bool IsFolded
    {
        get { return isFolded; }
        set { isFolded = value; }
    }




    void OnEnable()
    {
        InputListener.OnStepInput += HandleStepInput;
        //ZoneDetector.OnZoneEntered   += OnZoneDetected;
    }

    void OnDisable()
    {
        InputListener.OnStepInput -= HandleStepInput;
        //ZoneDetector.OnZoneEntered   -= OnZoneDetected;
    }

    // // ZoneDetector からの衝突検知を受け取るメソッド
    // void OnZoneDetected(int count)
    // {
    //     // 進行方向を反転
    //     isFolded = !isFolded;
        
    // }

    void HandleStepInput(bool isRightFoot)
    {
        StartHeadRemap();
    }

    void Update()
    {
        if (isRemapping) RemapHeadHeight();
    }

    void StartHeadRemap()
    {
        if (!isRemapping)
        {
            currentHeadHeight = headTransform.position.y;
            currentZPosition  = headTransform.position.z;
            t0 = Time.time;
            isRemapping = true;
        }
    }

    void RemapHeadHeight()
    {
        float elapsed = Time.time - t0;
        float delta   = stepDuration / (5f - 2f * omega);

        if (elapsed >= stepDuration)
        {
            isRemapping = false;
            return;
        }

        // isFolded の状態で前後移動量を反転
        float Depth = isFolded ? -stepDepth : stepDepth;

        float sigmoidY = 1f / (1f + Mathf.Exp(-transitionStiffnessHeadY * (elapsed - delta)));
        float sigmoidZ = 1f / (1f + Mathf.Exp(-transitionStiffnessHeadZ * (elapsed - delta)));

        float newY = currentHeadHeight + omega * stepHeight * sigmoidY;
        float newZ = currentZPosition  + omega * Depth  * sigmoidZ;

        headTransform.position = new Vector3(
            headTransform.position.x,
            newY,
            newZ
        );
    }
}
