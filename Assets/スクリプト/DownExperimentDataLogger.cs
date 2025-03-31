using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Valve.VR; // SteamVR関連クラスを使用

public class DownExperimentDataCollector : MonoBehaviour
{
    public enum ExperimentType { DownEMG, DownTracker, DownController, DownAlternating }
    public ExperimentType experimentType; 
    public string customFileName = "Experiment"; // 保存するファイル名をインスペクターで指定
    public float stepDuration = 0.8f; // デフォルト値を1秒に設定
    // BPMオブジェクトの参照
    public GameObject BPM60;
    public GameObject BPM90;
    public GameObject BPM120;
    public enum BpmType { Bpm60, Bpm90, Bpm120 }
    public BpmType bpmType;
    
    //HMD
    public Transform headTransform;
    public Transform rightShoe;
    public Transform leftShoe;

    // DownEMG専用
    public DownEMG_pulse downEMGScript;
    public GameObject downEMG;

    // DownTracker専用
    public DownTracker downTrackerScript;
    public GameObject downTracker;

    // DownController専用
    public DownController downControllerScript;
    public GameObject downContoller;
    private SteamVR_Action_Boolean GrabG = SteamVR_Actions.default_GrabGrip; // GrabGripボタンのアクション
    private SteamVR_Action_Boolean Iui = SteamVR_Actions.default_InteractUI;

    //DownAlternating専用
    public Down2Alternating down2Alternating;
    public GameObject downAlternating;


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

    // 保存先ディレクトリ（指定のパス）
    private string saveDirectory = @"C:\Users\Hirosue Yuta\OneDrive - 学校法人立命館\デスクトップ\ドキュメント\HirosueYuta\実験データ\生データ"; //DeskTop PC
    //private string saveDirectory = @"/Users/hiroshimatsuyuuta/Documents/研究室/卒論/環境/実験データ"; //MacBook

    void Start()
    {
        isReadyRecord = false;
        isRecording = false;
        audioSource = gameObject.GetComponent<AudioSource>();
        startTime = -1;

        switch (experimentType){
            case ExperimentType.DownEMG:
                downEMG.SetActive(true);
                downTracker.SetActive(false);
                downContoller.SetActive(false);
                downAlternating.SetActive(false);
                break;
            case ExperimentType.DownTracker:
                downEMG.SetActive(false);
                downTracker.SetActive(true);
                downContoller.SetActive(false);
                downAlternating.SetActive(false);
                break;
            case ExperimentType.DownController:
                downEMG.SetActive(false);
                downTracker.SetActive(false);
                downContoller.SetActive(true);
                downAlternating.SetActive(false);
                break;
            case ExperimentType.DownAlternating:
                downEMG.SetActive(false);
                downTracker.SetActive(false);
                downContoller.SetActive(false);
                downAlternating.SetActive(true);
                break;    
        }

        //BPMの設定
        switch (bpmType){
            case BpmType.Bpm60:
                stepDuration = 0.99f;
                BPM60.SetActive(true);
                BPM90.SetActive(false);
                BPM120.SetActive(false);
                break;
            case BpmType.Bpm90:
                stepDuration = 0.65f;
                BPM60.SetActive(false);
                BPM90.SetActive(true);
                BPM120.SetActive(false);
                break;
            case BpmType.Bpm120:
                stepDuration = 0.49f;
                BPM60.SetActive(false);
                BPM90.SetActive(false);
                BPM120.SetActive(true);
                break;
        
        }
        // 実験タイプごとに、このスクリプトのstepDurationに変更
        SetStepDuration();

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
            case ExperimentType.DownEMG:
                if (downEMGScript != null)
                {
                    isRightShoeTurn = downEMGScript.isRightShoeTurn;
                    isLeftShoeTurn = downEMGScript.isLeftShoeTurn;
                    emgLeft = downEMGScript.EMGDataLeft;
                    emgRight = downEMGScript.EMGDataRight;

                    // EMGトリガー判定
                    LeftemgTrigger = (
                                  (downEMGScript.isDetectLeftPeak && !downEMGScript.isRightFootNext) || 
                                  (downEMGScript.isDetectLeftPeak && downEMGScript.isRightFootNext)) ;
                        
                    RightemgTrigger = ((downEMGScript.isDetectRightPeak && downEMGScript.isRightFootNext) || 
                                  
                                  (downEMGScript.isDetectRightPeak && !downEMGScript.isRightFootNext)
                                  );
        
                }
                break;

            case ExperimentType.DownTracker:
                if (downTrackerScript != null)
                {
                    isRightShoeTurn = downTrackerScript.isRightShoeTurn;
                    isLeftShoeTurn = downTrackerScript.isLeftShoeTurn;
                    trackerLeft = downTrackerScript.RelativeHeightLeftTracker;
                    trackerRight = downTrackerScript.RelativeHeightRightTracker;

                    // トラッカートリガー判定 (初めてしきい値を超えたとき)
                    LefttrackerTrigger = ((downTrackerScript.isLeftFootDown && downTrackerScript.canTriggerLeft && !downTrackerScript.isRightFootNext) || 
                                            (downTrackerScript.isLeftFootDown && downTrackerScript.canTriggerLeft && downTrackerScript.isRightFootNext)  );
                    RighttrackerTrigger = ((downTrackerScript.isRightFootDown && downTrackerScript.canTriggerRight && downTrackerScript.isRightFootNext) || 
                                            (downTrackerScript.isRightFootDown && downTrackerScript.canTriggerRight && !downTrackerScript.isRightFootNext)) ;
                    
                }
                break;

            case ExperimentType.DownController:
                if (downControllerScript != null)
                {
                    isRightShoeTurn = downControllerScript.isRightShoeTurn;
                    isLeftShoeTurn = downControllerScript.isLeftShoeTurn;

                    // コントローラートリガー判定 (ボタン押下の瞬間)
                    controllerTrigger = Iui.GetStateDown(SteamVR_Input_Sources.LeftHand) || Iui.GetStateDown(SteamVR_Input_Sources.RightHand);
                }
                break;
            
            case ExperimentType.DownAlternating:
                if (down2Alternating != null)
                {
                    isRightShoeTurn = down2Alternating.isRightShoeTurn;
                    isLeftShoeTurn = down2Alternating.isLeftShoeTurn;

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
            case ExperimentType.DownEMG:
                if (downEMGScript != null)
                {
                    downEMGScript.stepDuration = stepDuration;
                }
                break;

            case ExperimentType.DownTracker:
                if (downTrackerScript != null)
                {
                    downTrackerScript.stepDuration = stepDuration;
                }
                break;

            case ExperimentType.DownController:
                if (downControllerScript != null)
                {
                    downControllerScript.stepDuration = stepDuration;
                }
                break;

            case ExperimentType.DownAlternating:
                if (down2Alternating != null)
                {
                    down2Alternating.stepDuration = stepDuration;
                }
                break;
        }
    }
}
