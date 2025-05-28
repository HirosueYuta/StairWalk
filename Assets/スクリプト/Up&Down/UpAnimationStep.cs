using UnityEngine;
using System; 

[RequireComponent(typeof(Animator))]
public class UpAnimationStep : MonoBehaviour
{
    Animator animator;
    bool firstStepDone = false;

    // ① 速度係数をフィールドとして持っておく
    //    （必要なら Inspector からも調整できます）
    [SerializeField, Tooltip("クリップを何秒で再生したいか")]
    float targetDuration = 0.8f;

    // クリップ本来の長さ（Inspector で確認済みなら直書きでも OK）
    const float clipOriginalLength = 0.917f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
    }

    void OnEnable()
    {
        // InputListener のイベントを購読
        InputListener.OnStepInput += HandleStepInput;
    }

    void OnDisable()
    {
        InputListener.OnStepInput -= HandleStepInput;
    }

    // InputListener から「右足(true)/左足(false)」を受け取る
    void HandleStepInput(bool isRightFoot)
    {
        // １歩目は起動直後に既に再生済みなのでスキップ
        if (!firstStepDone)
        {
            PlayFirstStep();
            return;
        }

        // ２歩目以降
        PlayRightStep(isRightFoot);
    }


    void Update()
    {
        // 起動時に１歩目
        if (!firstStepDone)
        {
            PlayFirstStep();
        }
    }

    void PlayFirstStep()
    {
        // １歩目だけ Root Motion を有効化
        animator.applyRootMotion = true;

        transform.localScale = Vector3.one;
        animator.speed = 1f;
        animator.Play("FirstStep", 0, 0f);
        firstStepDone = true;
    }

    void PlayRightStep(bool isRightFoot)
    {
        // ２歩目以降は必ず Root Motion を無効化
        animator.applyRootMotion = false;
        
        // ミラー設定：左足なら負スケール
        transform.localScale = isRightFoot
            ? Vector3.one
            : new Vector3(-1, 1, 1);

        // 再生速度を調整
        float speedFactor = clipOriginalLength / targetDuration;
        animator.speed = speedFactor;

        // アニメーション再生
        animator.Play("RightStep", 0, 0f);
    }
}
