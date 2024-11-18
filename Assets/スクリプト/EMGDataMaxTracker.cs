using UnityEngine;

public class EMGDataMaxTracker : MonoBehaviour
{
    public EMGDataReceiver emgDataReceiver; // EMGDataReceiverの参照

    [Header("最大値 (254未満のみ適用)")]
    public int maxEmgValue1 = 250; // emgValue1の最大値（初期値は250）
    public int maxEmgValue2 = 250; // emgValue2の最大値（初期値は250）

    private int highestEmgValue1 = 0; // 最初の10秒間での最高値
    private int highestEmgValue2 = 0; // 最初の10秒間での最高値
    private float startTime;          // 開始時刻
    private bool initialized = false; // 初期化フラグ
    private const int spikeThreshold = 50; // 急上昇を無視するしきい値

    void Start()
    {
        if (emgDataReceiver == null)
        {
            Debug.LogError("EMGDataReceiverが設定されていません！");
        }

        // 開始時刻を記録
        startTime = Time.time;

        // 初期値を設定
        highestEmgValue1 = 0;
        highestEmgValue2 = 0;
        maxEmgValue1 = 250;
        maxEmgValue2 = 250;
    }

    void Update()
    {
        if (emgDataReceiver == null) return;

        float elapsedTime = Time.time - startTime;

        // 最初の10秒間は最高値を記録するだけで最大値を更新しない
        if (elapsedTime < 10f)
        {
            RecordHighestValues();
        }
        else if (!initialized)
        {
            // 10秒経過後に最大値を設定
            maxEmgValue1 = highestEmgValue1;
            maxEmgValue2 = highestEmgValue2;
            initialized = true; // 初期化完了
            Debug.Log($"10秒経過後の最大値: maxEmgValue1={maxEmgValue1}, maxEmgValue2={maxEmgValue2}");
        }
        else
        {
            // 通常の最大値更新処理
            UpdateMaxValues();
        }
    }

    // 最初の10秒間の最高値を記録
    private void RecordHighestValues()
    {
        int currentEmgValue1 = emgDataReceiver.emgValue1;
        int currentEmgValue2 = emgDataReceiver.emgValue2;

        if (currentEmgValue1 < 254 && currentEmgValue1 > highestEmgValue1)
        {
            highestEmgValue1 = currentEmgValue1;
        }

        if (currentEmgValue2 < 254 && currentEmgValue2 > highestEmgValue2)
        {
            highestEmgValue2 = currentEmgValue2;
        }
    }

    // 通常の最大値更新処理
    private void UpdateMaxValues()
    {
        int currentEmgValue1 = emgDataReceiver.emgValue1;
        int currentEmgValue2 = emgDataReceiver.emgValue2;

        if (currentEmgValue1 < 254 && Mathf.Abs(currentEmgValue1 - maxEmgValue1) <= spikeThreshold)
        {
            if (currentEmgValue1 > maxEmgValue1)
            {
                maxEmgValue1 = currentEmgValue1;
            }
        }

        if (currentEmgValue2 < 254 && Mathf.Abs(currentEmgValue2 - maxEmgValue2) <= spikeThreshold)
        {
            if (currentEmgValue2 > maxEmgValue2)
            {
                maxEmgValue2 = currentEmgValue2;
            }
        }
    }
}
