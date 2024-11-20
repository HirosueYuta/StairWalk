using UnityEngine;
using System.Collections.Generic;

public class UpEMG_pulse : MonoBehaviour
{
    // 靴のTransformと階段動作に関連するパラメータ
    public Transform rightShoe;  
    public Transform leftShoe;   
    public float stepHeight = 0.18f;
    public float stepDepth = 0.29f;
    public float stepDuration = 0.8f; 
    public float curveStrength = 1.0f;  
    public float transitionStiffnessShoe = 10.0f;

    // 頭部（HMD）のTransformとリマッピングに関連するパラメータ
    public Transform headTransform;  
    public float transitionStiffnessHeadY = 12f;  
    public float transitionStiffnessHeadZ = 12f;  
    private float currentHeadHeight;  
    private float currentZPosition;   

    // 筋電位（EMG）データ受信用
    public EMGDataReceiver emgDataReceiver;  
    public float pulseThreshold = 50f;       // ピーク検出のための最小値
    public int peakDetectionWindow = 5;      // ピーク検出のためのウィンドウサイズ（点数）
    public int EMGDataLeft;
    public int EMGDataRight;

    // ステップ処理と状態管理
    private bool isStepping = false;  
    private bool isRightShoeTurn = true;
    private bool isFirstStep = true;
    private float progress = 0.0f;  
    private Vector3 startPosition;  
    private Vector3 targetPosition;

    // バッファ関連
    private bool isInputBuffered = false; // 入力バッファフラグ
    public float bufferActivationTime = 0.5f; // バッファを開始する時間

    // 頭部リマッピング管理
    private float t0;  
    private bool isRemapping = false; 
    private float omega = 1f;  

    // 筋電位データの履歴
    private List<float> rightEmgHistory = new List<float>(); 
    private List<float> leftEmgHistory = new List<float>();

    void Start()
    {
        currentHeadHeight = headTransform.position.y;
        currentZPosition = headTransform.position.z;
    }

    void Update()
    {
        // 筋電位のピークを検出
        DetectPeak();

        // ステップ中の場合、靴の位置を更新
        if (isStepping)
        {
            MoveShoe();

            // バッファリングを開始する時間を超えた場合、入力をバッファ
            if (progress >= bufferActivationTime && !isInputBuffered)
            {
                BufferInput();
            }
        }

        // バッファ処理：ステップ終了後に次のステップを開始
        if (!isStepping && isInputBuffered)
        {
            isInputBuffered = false;
            StartStep();
            StartHeadRemap();
        }

        // リマッピングが進行中の場合、頭部の位置を更新
        if (isRemapping)
        {
            RemapHeadHeight();
        }
    }

    // 筋電位のピークを検出する
    void DetectPeak()
    {
        float currentRightEmgValue = emgDataReceiver.emgValue1;
        float currentLeftEmgValue = emgDataReceiver.emgValue2;


        rightEmgHistory.Add(currentRightEmgValue);
        leftEmgHistory.Add(currentLeftEmgValue);

        // 履歴サイズを維持
        if (rightEmgHistory.Count > peakDetectionWindow) rightEmgHistory.RemoveAt(0);
        if (leftEmgHistory.Count > peakDetectionWindow) leftEmgHistory.RemoveAt(0);

        // ピーク検出（右足）
        if (isRightShoeTurn && rightEmgHistory.Count == peakDetectionWindow)
        {
            if (IsPeak(rightEmgHistory) && currentRightEmgValue >= pulseThreshold)
            {
                HandleInput();
            }
        }

        // ピーク検出（左足）
        if (!isRightShoeTurn && leftEmgHistory.Count == peakDetectionWindow)
        {
            if (IsPeak(leftEmgHistory) && currentLeftEmgValue >= pulseThreshold)
            {
                HandleInput();
            }
        }
    }

    // 入力を処理する
    void HandleInput()
    {
        if (!isStepping && !isRemapping)
        {
            StartStep();
            StartHeadRemap();
        }
        else if (isStepping && progress >= bufferActivationTime)
        {
            isInputBuffered = true;
        }
    }

    // ピークを検出する
    bool IsPeak(List<float> emgHistory)
    {
        int middleIndex = emgHistory.Count / 2;
        float middleValue = emgHistory[middleIndex];

        for (int i = 0; i < emgHistory.Count; i++)
        {
            if (i != middleIndex && emgHistory[i] >= middleValue)
            {
                return false;
            }
        }
        return true;
    }

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
            targetPosition = rightShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
        }
        else
        {
            startPosition = leftShoe.position;
            targetPosition = leftShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
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
        else
        {
            leftShoe.position = currentPosition;
        }

        if (progress >= 1.0f)
        {
            isStepping = false;
            isRightShoeTurn = !isRightShoeTurn;
            isFirstStep = false;
        }
    }

    // 頭部リマッピングを開始する
    void StartHeadRemap()
    {
        if (!isRemapping)
        {
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

    // 入力をバッファする
    void BufferInput()
    {
        if (isRightShoeTurn && IsPeak(rightEmgHistory))
        {
            isInputBuffered = true;
        }
        else if (!isRightShoeTurn && IsPeak(leftEmgHistory))
        {
            isInputBuffered = true;
        }
    }
}
