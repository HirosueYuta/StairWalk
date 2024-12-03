using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Valve.VR; // SteamVR関連クラスを使用

public class ExperimentDataCollector : MonoBehaviour
{
    public enum ExperimentType { UpEMG, UpTracker, UpController, UpAlternating }
    public ExperimentType experimentType; 
    public string customFileName = "Experiment"; // 保存するファイル名をインスペクターで指定
    [SerializeField] private float stepDuration = 0.8f; // デフォルト値を1秒に設定
    // BPMオブジェクトの参照
    public GameObject BPM50;
    public GameObject BPM75;
    public GameObject BPM120;
    public enum BpmType { Bpm50, Bpm75, Bpm120 }
    public BpmType bpmType;
    
    //HMD
    public Transform headTransform;
    public Transform rightShoe;
    public Transform leftShoe;

    // UpEMG専用
    public UpEMG_pulse upEMGScript;
    public GameObject upEMG;

    // UpTracker専用
    public UpTracker upTrackerScript;
    public GameObject upTracker;

    // UpController専用
    public UpController upControllerScript;
    public GameObject upContoller;
    private SteamVR_Action_Boolean GrabG = SteamVR_Actions.default_GrabGrip; // GrabGripボタンのアクション
    private SteamVR_Action_Boolean Iui = SteamVR_Actions.default_InteractUI;

    //UpAlternating専用
    public Up2Alternating up2Alternating;
    public GameObject upAlternating;


    // 音声
    public AudioClip startSound;  // 記録開始時の音声
    public AudioClip stopSound;   // 記録終了時の音声
    private AudioSource audioSource;

    // データ収集用
    private List<string> collectedData = new List<string>();
    private float startTime;
    [SerializeField] public bool isReadyRecord = false; // 記録する準備ができたか
    [SerializeField] public bool isRecording = false; // 記録中かどうか
    private float recordingDuration = 60f; // 記録時間（秒）

    // トリガー状態の追跡
    public bool prevEmgTrigger = false;
    public bool prevTrackerTrigger = false;
    public bool prevControllerTrigger = false;

    // 保存先ディレクトリ（指定のパス）
    private string saveDirectory = @"C:\Users\Hirosue Yuta\OneDrive - 学校法人立命館\デスクトップ\ドキュメント\HirosueYuta\実験データ"; //DeskTop PC
    //private string saveDirectory = @"/Users/hiroshimatsuyuuta/Documents/研究室/卒論/環境/実験データ"; //MacBook

    void Start()
    {
        isReadyRecord = false;
        isRecording = false;
        audioSource = gameObject.GetComponent<AudioSource>();
        startTime = -1;

        switch (experimentType){
            case ExperimentType.UpEMG:
                upEMG.SetActive(true);
                upTracker.SetActive(false);
                upContoller.SetActive(false);
                upAlternating.SetActive(false);
                break;
            case ExperimentType.UpTracker:
                upEMG.SetActive(false);
                upTracker.SetActive(true);
                upContoller.SetActive(false);
                upAlternating.SetActive(false);
                break;
            case ExperimentType.UpController:
                upEMG.SetActive(false);
                upTracker.SetActive(false);
                upContoller.SetActive(true);
                upAlternating.SetActive(false);
                break;
            case ExperimentType.UpAlternating:
                upEMG.SetActive(false);
                upTracker.SetActive(false);
                upContoller.SetActive(false);
                upAlternating.SetActive(true);
                break;    
        }

        // 実験タイプに応じた stepDuration の変更
        SetStepDuration();

        //BPMの設定
        switch (bpmType){
            case BpmType.Bpm50:
                BPM50.SetActive(true);
                BPM75.SetActive(false);
                BPM120.SetActive(false);
                break;
            case BpmType.Bpm75:
                BPM50.SetActive(false);
                BPM75.SetActive(true);
                BPM120.SetActive(false);
                break;
            case BpmType.Bpm120:
                BPM50.SetActive(false);
                BPM75.SetActive(false);
                BPM120.SetActive(true);
                break;
        }

        // CSVのヘッダーを追加
        collectedData.Add("Time,Head_Y,RightShoe_Y,LeftShoe_Y,RightShoeTurn,LeftShoeTurn,EMG_Left,EMG_Right,Tracker_Left,Tracker_Right,LeftEMG_Trigger,RightEMG_Trigger,LeftTracker_Trigger,RightTracker_Trigger,Controller");
    }

    void Update()
    {
        // 記録中のデータ収集
        if (isReadyRecord) //スタート時にインスペクターでtrueに変更
        {
            StartRecording();

            if (isRecording){
                float CountTime = Time.time - startTime;
                // 記録時間を超えたら停止
                if (CountTime >= recordingDuration)
                {
                    StopRecording();
                    return;
                }
                // トリガーデータを収集して記録
                CollectData(CountTime);
            }
        }
    }

    public void StartRecording()
    {
        if (startTime == -1){
            startTime = Time.time; // 記録開始時刻を設定

                    // 開始音を再生
        if (startSound != null)
        {
            audioSource.PlayOneShot(startSound);
        }

        Debug.Log("Recording started");

        isRecording  = true;
        }

    }

    private void StopRecording()
    {
        isRecording = false;

        // 終了音を再生
        if (stopSound != null)
        {
            audioSource.PlayOneShot(stopSound);
        }

        Debug.Log("Recording stopped");

        // データ保存
        SaveDataToFile();
        isReadyRecord = false;
    }

    private void CollectData(float elapsedTime)
    {
        float headY = headTransform.position.y;
        float rightShoeY = rightShoe.position.y;
        float leftShoeY = leftShoe.position.y;

        bool isRightShoeTurn = false;
        bool isLeftShoeTurn = false;

        float emgLeft = 0;
        float emgRight = 0;
        float trackerLeft = 0;
        float trackerRight = 0;

        //bool emgTrigger = false;
        bool RightemgTrigger = false;
        bool LeftemgTrigger = false;
        //bool trackerTrigger = false;
        bool LefttrackerTrigger = false;
        bool RighttrackerTrigger = false;
        bool controllerTrigger = false;

        switch (experimentType)
        {
            case ExperimentType.UpEMG:
                if (upEMGScript != null)
                {
                    isRightShoeTurn = upEMGScript.isRightShoeTurn;
                    isLeftShoeTurn = upEMGScript.isLeftShoeTurn;
                    emgLeft = upEMGScript.EMGDataLeft;
                    emgRight = upEMGScript.EMGDataRight;

                    // EMGトリガー判定
                    LeftemgTrigger = (
                                  (upEMGScript.isDetectLeftPeak && !upEMGScript.isRightFootNext) || 
                                  (upEMGScript.isDetectLeftPeak && upEMGScript.isRightFootNext)) ;
                        
                    RightemgTrigger = ((upEMGScript.isDetectRightPeak && upEMGScript.isRightFootNext) || 
                                  
                                  (upEMGScript.isDetectRightPeak && !upEMGScript.isRightFootNext)
                                  );
        
                }
                break;

            case ExperimentType.UpTracker:
                if (upTrackerScript != null)
                {
                    isRightShoeTurn = upTrackerScript.isRightShoeTurn;
                    isLeftShoeTurn = upTrackerScript.isLeftShoeTurn;
                    trackerLeft = upTrackerScript.RelativeHeightLeftTracker;
                    trackerRight = upTrackerScript.RelativeHeightRightTracker;

                    // トラッカートリガー判定 (初めてしきい値を超えたとき)
                    LefttrackerTrigger = ((upTrackerScript.isLeftFootUp && upTrackerScript.canTriggerLeft && !upTrackerScript.isRightFootNext) || 
                                            (upTrackerScript.isLeftFootUp && upTrackerScript.canTriggerLeft && upTrackerScript.isRightFootNext)  );
                    RighttrackerTrigger = ((upTrackerScript.isRightFootUp && upTrackerScript.canTriggerRight && upTrackerScript.isRightFootNext) || 
                                            (upTrackerScript.isRightFootUp && upTrackerScript.canTriggerRight && !upTrackerScript.isRightFootNext)) ;
                    
                }
                break;

            case ExperimentType.UpController:
                if (upControllerScript != null)
                {
                    isRightShoeTurn = upControllerScript.isRightShoeTurn;
                    isLeftShoeTurn = upControllerScript.isLeftShoeTurn;

                    // コントローラートリガー判定 (ボタン押下の瞬間)
                    controllerTrigger = Iui.GetStateDown(SteamVR_Input_Sources.LeftHand) || Iui.GetStateDown(SteamVR_Input_Sources.RightHand);
                    prevControllerTrigger = GrabG.GetStateDown(SteamVR_Input_Sources.LeftHand) || GrabG.GetStateDown(SteamVR_Input_Sources.RightHand);
                }
                break;
            
            case ExperimentType.UpAlternating:
                if (up2Alternating != null)
                {
                    isRightShoeTurn = up2Alternating.isRightShoeTurn;
                    isLeftShoeTurn = up2Alternating.isLeftShoeTurn;

                    //controllerTrigger = Input.GetKeyDown(KeyCode.A)|| Input.GetKeyDown(KeyCode.D);
                }
                break;                
        }

        // データを追加
        string data = $"{elapsedTime:F2},{headY:F3},{rightShoeY:F3},{leftShoeY:F3},{isRightShoeTurn},{isLeftShoeTurn},{emgLeft:F3},{emgRight:F3},{trackerLeft:F3},{trackerRight:F3},{(LeftemgTrigger ? 1 : 0)},{(RightemgTrigger ? 1 : 0)},{(LefttrackerTrigger ? 1 : 0)},{(RighttrackerTrigger ? 1 : 0)},{(controllerTrigger ? 1 : 0)}";
        collectedData.Add(data);
        Debug.Log(data); // デバッグ用ログ
    }

    private void SaveDataToFile()
    {
        // ファイル名を実験タイプ＋インスペクター指定名で設定
        string fileName = $"{experimentType}_{bpmType}_{customFileName}_{Time.time}.csv";

        // 保存先ディレクトリの確認と作成
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        // フルパスで保存
        string filePath = Path.Combine(saveDirectory, fileName);

        // データをCSV形式で保存
        File.WriteAllLines(filePath, collectedData);
        Debug.Log($"Data saved to {filePath}");
    }

    void SetStepDuration(){
        switch (experimentType)
        {
            case ExperimentType.UpEMG:
                if (upEMGScript != null)
                {
                    upEMGScript.stepDuration = stepDuration;
                }
                break;

            case ExperimentType.UpTracker:
                if (upTrackerScript != null)
                {
                    upTrackerScript.stepDuration = stepDuration;
                }
                break;

            case ExperimentType.UpController:
                if (upControllerScript != null)
                {
                    upControllerScript.stepDuration = stepDuration;
                }
                break;

            case ExperimentType.UpAlternating:
                if (up2Alternating != null)
                {
                    up2Alternating.stepDuration = stepDuration;
                }
                break;
        }
    }
}
