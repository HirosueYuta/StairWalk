using UnityEngine;
using Valve.VR; // SteamVR関連クラスを使用

public class UpController : MonoBehaviour
{
    public Transform rightShoe;          // 右靴のTransform
    public Transform leftShoe;           // 左靴のTransform
    public Transform headTransform;      // HMD（頭部）のTransform

    public float stepHeight = 0.18f;     // ステップの高さ
    public float stepDepth = 0.29f;      // ステップの奥行き
    public float stepDuration = 0.8f;    // ステップの継続時間
    public float curveStrength = 1.0f;   // ステップの曲線強度
    public float transitionStiffnessShoe = 10.0f; // 靴移動の滑らかさ

    public float transitionStiffnessHeadY = 12f;  // 頭部Y軸リマッピングの滑らかさ
    public float transitionStiffnessHeadZ = 12f;  // 頭部Z軸リマッピングの滑らかさ
    private float currentHeadHeight;             // 現在の頭部高さ
    private float currentZPosition;              // 現在の頭部Z位置

    private SteamVR_Action_Boolean GrabG = SteamVR_Actions.default_GrabGrip; // GrabGripボタンのアクション
    private bool grapgripLeftHand;
    private bool grapgripRightHand;

    private bool isStepping = false;    // ステップ中かどうか
    [SerializeField]
    private bool isRightShoeTurn = false;
    [SerializeField]
    private bool isLeftShoeTurn = false;
    private bool isFirstStep = true;
    private float progress = 0.0f;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float t0;
    private bool isRemapping = false;
    private float omega = 1f;

    // バッファリング関連
    private bool isInputBuffered = false;        // 入力がバッファされたかどうか
    public float bufferTimeThreshold = 0.5f;    // 入力をバッファする進行度
    private bool bufferedRightHand = false;      // 右手の入力がバッファされたか
    private bool bufferedLeftHand = false;       // 左手の入力がバッファされたか

    void Start()
    {
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;

        isRightShoeTurn = false;
        isLeftShoeTurn = false;
    }

    void Update()
    {
        // SteamVRコントローラーの入力を取得
        grapgripLeftHand = GrabG.GetStateDown(SteamVR_Input_Sources.LeftHand);
        grapgripRightHand = GrabG.GetStateDown(SteamVR_Input_Sources.RightHand);

        if(grapgripRightHand)
        {
            isRightShoeTurn = true;
            StartStep();
        }

        if(grapgripLeftHand)
        {
            isLeftShoeTurn = true;
            StartStep();
        }

        if(isStepping){
            MoveShoe();
            ///終わったらMoveShoe内で
            ///isrightshoueturn = false or isleftshoueturn = false
            ///isStepping = false;
        }

        if (isRemapping){
            RemapHeadHeight();
        }

        // // ステップ中の処理
        // if (isStepping)
        // {
        //     MoveShoe();

        //     // ステップ中の入力をバッファ
        //     if (progress >= bufferTimeThreshold)
        //     {
        //         if (isRightShoeTurn && grapgripLeftHand )
        //         {
        //             bufferedLeftHand = true;
        //             isInputBuffered = true;
        //         }
        //         else if (!isRightShoeTurn && grapgripRightHand)
        //         {
        //             bufferedRightHand = true;
        //             isInputBuffered = true;
        //         }
        //     }
        // }

        // // ステップ終了後にバッファされた入力を処理
        // if (!isStepping && !isRemapping && isInputBuffered)
        // {
        //     isInputBuffered = false; // バッファをクリア
            
        //     if (bufferedRightHand && isRightShoeTurn)
        //     {
        //         StartStep();
        //         StartHeadRemap();
        //     }
        //     else if (bufferedLeftHand && !isRightShoeTurn)
        //     {
        //         StartStep();
        //         StartHeadRemap();
        //     }

        //     // バッファ状態をリセット
        //     bufferedRightHand = false;
        //     bufferedLeftHand = false;
        // }

        // // 通常の入力処理
        // if (!isStepping && !isRemapping)
        // {
        //     if (isRightShoeTurn && grapgripRightHand)
        //     {
        //         StartStep();
        //         StartHeadRemap();
        //     }
        //     else if (!isRightShoeTurn && grapgripLeftHand)
        //     {
        //         StartStep();
        //         StartHeadRemap();
        //     }
        // }

        // // リマッピング処理
        // if (isRemapping)
        // {
        //     RemapHeadHeight();
        // }
    }

    void StartStep()
    {
        isStepping = true;
        progress = 0.0f;

        float heightMultiplier = isFirstStep ? 1.0f : 2.0f;
        float depthMultiplier = isFirstStep ? 1.0f : 2.0f;

        if (isRightShoeTurn)
        {
            startPosition = rightShoe.position;
            targetPosition = rightShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
        }
        else if (isLeftShoeTurn)
        {
            startPosition = leftShoe.position;
            targetPosition = leftShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
        }
        
        // 頭部リマッピングを開始
        StartHeadRemap();
    }

    void MoveShoe()
    {
        progress += Time.deltaTime / stepDuration;
        progress = Mathf.Clamp01(progress);

        float sigmoidProgress = 1 / (1 + Mathf.Exp(-transitionStiffnessShoe * (progress - 0.5f)));
        Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, sigmoidProgress);
        currentPosition.y += Mathf.Sin(sigmoidProgress * Mathf.PI) * stepHeight * curveStrength;

        if (isRightShoeTurn)
        {
            rightShoe.position = currentPosition;
        }
        else if (isLeftShoeTurn)
        {
            leftShoe.position = currentPosition;
        }

        if (progress >= 1.0f)
        {
            isStepping = false;

            //ステップ完了後にターンをリセット
            if (isRightShoeTurn)
            {
                isRightShoeTurn = false;
                Debug.Log("右足のステップが完了しました");
            }
            else if (isLeftShoeTurn)
            {
                isLeftShoeTurn = false;
                Debug.Log("左足のステップが完了しました");
            }
            isFirstStep = false;
        }
    }

    void StartHeadRemap()
    {
        if (!isRemapping)
        {
            t0 = Time.time;
            isRemapping = true;
        }
    }

    void RemapHeadHeight()
    {
        float currentTime = Time.time - t0;
        float delta = stepDuration / (5f - 2f * omega);

        if (currentTime >= stepDuration)
        {
            isRemapping = false;
            currentHeadHeight = headTransform.position.y;
            currentZPosition = headTransform.position.z;
        }
        else
        {
            float newHeight = currentHeadHeight + omega * stepHeight * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadY * (currentTime - delta))));
            float newZPosition = currentZPosition + omega * stepDepth * (1 / (1 + Mathf.Exp(-transitionStiffnessHeadZ * (currentTime - delta))));

            headTransform.position = new Vector3(headTransform.position.x, newHeight, newZPosition);
        }
    }
}
