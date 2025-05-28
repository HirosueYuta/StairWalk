using UnityEngine;
using System;
using Valve.VR;  // SteamVR の名前空間

public class InputListener : MonoBehaviour
{
    public enum InputMode
    {
        Keyboard,
        Controller
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

    // true=右足, false=左足 の入力イベント
    public static event Action<bool> OnStepInput;

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
}
