using UnityEngine;
using System;
using Valve.VR;  // SteamVR の名前空間

public class InputListener : MonoBehaviour
{
    public enum InputMode
    {
        Keyboard,
        Controller,
        Tracker,
        EMG
    }

    [Header("入力モード切替")]
    public InputMode inputMode = InputMode.Keyboard;

    [Header("Keyboard モード設定")]
    public KeyCode rightKey = KeyCode.D;
    public KeyCode leftKey  = KeyCode.A;

    [Header("Controller モード設定")]
    public SteamVR_Action_Boolean controllerAction = SteamVR_Actions.default_InteractUI;
    public SteamVR_Input_Sources rightHandSource = SteamVR_Input_Sources.RightHand;
    public SteamVR_Input_Sources leftHandSource  = SteamVR_Input_Sources.LeftHand;

    [Header("Tracker モード設定")]
    [Tooltip("右足につけた Tracked Object の Transform")]
    public Transform rightTrackerTransform;
    [Tooltip("左足につけた Tracked Object の Transform")]
    public Transform leftTrackerTransform;

    // 前フレームの Y 座標を保持するための変数
    private float prevRightY;
    private float prevLeftY;
    [Tooltip("足を持ち上げたと判定するための最小上昇量")]
    public float raiseThreshold = 0.01f;
    // [Tooltip("チェックするための最大高さ（m）")]
    // public float maxAllowHeight = 0.3f;

    [Header("EMG モード設定")]
    public EMGDataCalibrator emgDataCalibrator;  
    public float pulseThreshold = 25f;       // ピーク検出のための最小値
    [SerializeField]public float EMGDataLeft;
    [SerializeField]public float EMGDataRight;

    // true=右足, false=左足 の入力イベント
    public static event Action<bool> OnStepInput;

    void Start()
    {
        // 起動時に、もし Tracker モードで使うなら初期の Y 座標をキャッシュしておく
        if (rightTrackerTransform != null)
            prevRightY = rightTrackerTransform.localPosition.y;
        if (leftTrackerTransform != null)
            prevLeftY = leftTrackerTransform.localPosition.y;
    }
    
    void Update()
    {
        switch (inputMode)
        {
            case InputMode.Keyboard:
                CheckKeyboard();
                break;

            case InputMode.Controller:
                CheckController();
                break;

            case InputMode.Tracker:
                CheckTracker();
                break;

            case InputMode.EMG:
                CheckEMG();
                break;
        }
    }

    void CheckKeyboard()
    {
        if (Input.GetKeyDown(rightKey))
        {
            OnStepInput?.Invoke(true);
        }
        else if (Input.GetKeyDown(leftKey))
        {
            OnStepInput?.Invoke(false);
        }
    }

    void CheckController()
    {
        bool rightDown = controllerAction.GetStateDown(rightHandSource);
        bool leftDown  = controllerAction.GetStateDown(leftHandSource);

        if (rightDown)
        {
            OnStepInput?.Invoke(true);
        }
        else if (leftDown)
        {
            OnStepInput?.Invoke(false);
        }
    }

    void CheckTracker()
    {
        if (rightTrackerTransform == null || leftTrackerTransform == null)
        {
            Debug.LogWarning("[InputListener] Tracker モードが選択されていますが、right/left Tracker の Transform が設定されていません。");
            return;
        }

        // 現フレームのローカル Y 座標を取得
        float currentRightY = rightTrackerTransform.localPosition.y;
        float currentLeftY  = leftTrackerTransform.localPosition.y;

        //  === 右足トラッカーの判定 ===
        // 前フレームよりも Y が上がった（currentRightY - prevRightY > 閾値）かつ、
        // その高さが 0.3m 以下の場合に限り「右足のステップ入力」とみなす
        //if (currentRightY - prevRightY > raiseThreshold && currentRightY <= maxAllowHeight)
        if (currentRightY - prevRightY > raiseThreshold )
        {
            OnStepInput?.Invoke(true);
        }

        //  === 左足トラッカーの判定 ===
        if (currentLeftY - prevLeftY > raiseThreshold )
        {
            OnStepInput?.Invoke(false);
        }

        // 今フレームの Y を次回フレーム用にキャッシュしておく
        prevRightY = currentRightY;
        prevLeftY  = currentLeftY;
    }

    void CheckEMG()
    {

    }
}
