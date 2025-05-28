using UnityEngine;
using System;  // Action<bool> のため

[RequireComponent(typeof(Animator))]
public class DownAnimationStep : MonoBehaviour
{
    Animator animator;
    bool firstStepDone = false;

    [SerializeField, Tooltip("クリップを何秒で再生したいか")]
    float targetDuration = 0.5f;

    // DownStep アニメーションクリップの元の長さ（秒）
    const float clipOriginalLength = 0.5f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
    }

    void OnEnable()
    {
        // InputListener のステップ入力イベントを購読
        InputListener.OnStepInput += HandleStepInput;
    }

    void OnDisable()
    {
        InputListener.OnStepInput -= HandleStepInput;
    }

    void Update()
    {
        // 起動時に１歩目を自動再生
        if (!firstStepDone)
        {
            PlayFirstStep();
        }
    }

    // イベントで呼ばれるハンドラ
    void HandleStepInput(bool isRightFoot)
    {
        // １歩目がまだならそちらを優先
        if (!firstStepDone)
        {
            PlayFirstStep();
            return;
        }

        // ２歩目以降は常に RightStep を使い、左右ミラーだけ切り替え
        PlayRightStep(isRightFoot);
    }

    void PlayFirstStep()
    {
        transform.localScale = Vector3.one;             // 正しい向き
        animator.speed = 1f;
        animator.Play("FirstStep", 0, 0f);
        firstStepDone = true;
    }

    void PlayRightStep(bool isRightFoot)
    {
        // 左足入力ならミラー
        transform.localScale = isRightFoot
            ? Vector3.one       // 右足なら通常向き
            : new Vector3(-1,1,1); // 左足なら X を反転

        float speedFactor = clipOriginalLength / targetDuration;
        animator.speed = speedFactor;

        animator.Play("RightStep", 0, 0f);
    }
}
