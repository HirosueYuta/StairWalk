using UnityEngine;
using System.Collections.Generic;

public class DownEMG_pulse : MonoBehaviour
{
    public Transform rightShoe;  
    public Transform leftShoe;   
    public Transform headTransform;// HMD（頭部）のTransform
    public Transform headCamera;  //HMDの位置

    // 靴のTransformと階段動作に関連するパラメータ
    private float stepHeight = 0.17995f;
    private float stepDepth = 0.29f;
    public float stepDuration = 0.8f; 
    private float curveStrength = 1.0f;  
    private float transitionStiffnessShoe = 10.0f;

    // 頭部（HMD）のTransformとリマッピングに関連するパラメータ
    private float transitionStiffnessHeadY = 12f;  
    private float transitionStiffnessHeadZ = 12f;  
    private float currentHeadHeight;  
    private float currentZPosition;   

    // 筋電位（EMG）データ受信用
    public EMGDataCalibrator emgDataCalibrator;  
    public float pulseThreshold = 25f;       // ピーク検出のための最小値
    //private int peakDetectionWindow = 5;      // ピーク検出のためのウィンドウサイズ（点数）
    [SerializeField]
    public float EMGDataLeft;
    [SerializeField]
    public float EMGDataRight;
    
    // 筋電位データの履歴
    private List<float> rightEmgHistory = new List<float>(); 
    private List<float> leftEmgHistory = new List<float>();

    // ステップ処理と状態管理
    private bool isStepping = false;  
    public bool isRightFootNext = true;
    public bool isRightShoeTurn = false;
    public bool isLeftShoeTurn = false;
    private bool isFirstStep = false;
    public bool isDetectLeftPeak = false;       //左足の筋電位のピークを検出したかどうか
    public bool isDetectRightPeak = false;      //右足の筋電位のピークを検出したかどうか
    private float progress = 0.0f;  
    private Vector3 startPosition;  
    private Vector3 targetPosition;

    // 頭部リマッピング管理
    private float t0;  
    private bool isRemapping = false; 
    private float omega =-1f;  

    // 入力バッファ
    private bool bufferedLeftInput = false;
    private bool bufferedRightInput = false;

    //初期位置合わせ
    private bool isInitialHeadPositiontSet = false;
    private float initialHeadPositionX = 0f;
    private float initialHeadPositionZ = 0f;

    void Start()
    {
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;

        isLeftShoeTurn = false;
        isRightShoeTurn = false;
        isRightFootNext = true;

        // スタート時に右足を一段分上げる
       //ightShoe.position = new Vector3(rightShoe.position.x, rightShoe.position.y+stepHeight, rightShoe.position.z+stepDepth);
        //isFirstStep = false;               
    }

    void Update()
    {
        //初期位置調整
        if (!isInitialHeadPositiontSet){
            SetInitialHeadPosition();
        }

        //筋電位
        IsOverThreshold();

        // ステップ中の場合、靴の位置を更新
        if (!isStepping)
        {
            // 通常の入力を処理
            if (isDetectRightPeak && isRightFootNext)//右足の閾値が超えたら、左足を上げる
            {
                isLeftShoeTurn = true;//左足が動く
                StartStep();
                StartHeadRemap();
            }
            else if (isDetectLeftPeak && !isRightFootNext)
            {
                isRightShoeTurn = true;
                StartStep();
                StartHeadRemap();
            }
        }
        else
        {
            if (isDetectRightPeak && !isRightFootNext)
            {
                bufferedRightInput = true;
            }
            else if (isDetectLeftPeak && isRightFootNext)
            {
                bufferedLeftInput = true;
            }
        }

        // ステップ処理
        if (isStepping)
        {
            MoveShoe();
        }

        // 頭部リマッピング
        if (isRemapping)
        {
            RemapHeadHeight();
        }
    }

    void SetInitialHeadPosition(){
    //頭の初期位置調整
        initialHeadPositionX = headCamera.position.x;
        initialHeadPositionZ = headCamera.position.z; 

        if (!isInitialHeadPositiontSet && ( initialHeadPositionX != 0 || initialHeadPositionZ != 0)){
            headTransform.position = new Vector3(headTransform .position.x - headCamera.position.x, headTransform.position.y, headTransform.position.z-headCamera.position.z+58);
            Debug.Log(initialHeadPositionX+","+initialHeadPositionZ);
            isInitialHeadPositiontSet = true; // 初期高さの取得を完了
            }
        }

    void IsOverThreshold(){
        // 筋電位データを更新
        EMGDataLeft = emgDataCalibrator.calibratedEmgValue2;
        EMGDataRight = emgDataCalibrator.calibratedEmgValue1;

        isDetectLeftPeak = EMGDataLeft>pulseThreshold;
        isDetectRightPeak = EMGDataRight>pulseThreshold;
    }

    // void ProcessStepInput(){
    //     //通常の入力を処理
    //     if (isDetectLeftPeak && !isRightFootNext)
    //     {
    //         isLeftShoeTurn = true;
    //         StartStep();
    //         StartHeadRemap();
    //     }
    //     else if (isDetectRightPeak && isRightFootNext)
    //     {
    //         isRightShoeTurn = true;
    //         StartStep();
    //         StartHeadRemap();
    //     }
    // }

    // void StoreBufferedInput()
    // {
    //     if (isDetectLeftPeak && isRightFootNext)
    //     {
    //         bufferedLeftInput = true;
    //     }
    //     else if (isDetectRightPeak && !isRightFootNext)
    //     {
    //         bufferedRightInput = true;
    //     }
    // }



    // 新しい靴のステップを開始する
    void StartStep()
    {
        isStepping = true;
        progress = 0.0f;

        float heightMultiplier = isFirstStep ? 1.0f : 2.0f;
        float depthMultiplier = isFirstStep ? 1.0f : 2.0f;

        if (isRightShoeTurn)
        {
            startPosition = rightShoe.position;
            targetPosition = rightShoe.position + new Vector3(0,-stepHeight * heightMultiplier,-stepDepth * depthMultiplier);
        }
        else
        {
            startPosition = leftShoe.position;
            targetPosition = leftShoe.position + new Vector3(0,-stepHeight * heightMultiplier,-stepDepth * depthMultiplier);
        }
    }

    // 靴のステップ移動処理
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
            isRightFootNext = !isRightFootNext;

            // ステップ完了後にターンをリセット
            if (isRightShoeTurn)
            {
                isRightShoeTurn = false;
            }
            else if (isLeftShoeTurn)
            {
                isLeftShoeTurn = false;
            }

            isFirstStep = false;

            // バッファ処理
            ProcessBufferedInput();
        }
    }
    void ProcessBufferedInput()
    {
        if (bufferedRightInput && isRightFootNext)
        {
            bufferedRightInput = false;
            isLeftShoeTurn = true;
            
            // 頭部リマッピングを再リセットして再開
            ResetHeadRemap();

            StartStep();
            StartHeadRemap();
        }
        else if (bufferedLeftInput && !isRightFootNext)
        {
            bufferedLeftInput = false;
            isRightShoeTurn = true;

            // 頭部リマッピングを再リセットして再開
            ResetHeadRemap();

            StartStep();
            StartHeadRemap();
        }
    }


    // 頭部リマッピングを開始する
    void StartHeadRemap()
    {
        if (!isRemapping)
        {
            // 頭部の現在位置をリマッピングの基準として保存
            currentHeadHeight = headTransform.position.y;
            currentZPosition = headTransform.position.z;
            
            t0 = Time.time;
            isRemapping = true;
        }
    }

    // 頭部リマッピング処理
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
    void ResetHeadRemap()
    {
        isRemapping = false; // リマッピング状態をリセット
        t0 = Time.time;      // 現在の時刻を再設定
    }
}
