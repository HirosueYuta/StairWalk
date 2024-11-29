using UnityEngine;

public class EMGDataCalibrator : MonoBehaviour
{
    public EMGDataReceiver dataReceiver; // EMGDataReceiverの参照
    [Header("Calibration Settings")]
    public int maxEmgValue1 = 0; // キャリブレーション用の最大値1 
    public int maxEmgValue2 = 0; // キャリブレーション用の最大値2 
    public int minEmgValue1 = 254;// キャリブレーション用の最小値1 
    public int minEmgValue2 = 254;// キャリブレーション用の最小値２ 

    [Header("Calibrated EMG Data")]
    [Range(0, 100)]
    public float calibratedEmgValue1; // キャリブレーション後のEMG値1
    [Range(0, 100)]
    public float calibratedEmgValue2; // キャリブレーション後のEMG値2

    void Update()
    {
        if (dataReceiver != null)
        {
            // EMGDataReceiverから現在の値を取得
            int rawEmgValue1 = dataReceiver.emgValue1;
            int rawEmgValue2 = dataReceiver.emgValue2;

            // キャリブレーション処理
            calibratedEmgValue1 = Calibrate(rawEmgValue1, minEmgValue1, maxEmgValue1);
            calibratedEmgValue2 = Calibrate(rawEmgValue2, minEmgValue2, maxEmgValue2);
        }
        else
        {
            Debug.LogWarning("EMGDataReceiver reference is not set!");
        }
    }

    /// <summary>
    /// 入力値を0～100の範囲にスケーリングする（最小値と最大値を考慮）
    /// </summary>
    /// <param name="rawValue">生のEMG値</param>
    /// <param name="minValue">キャリブレーション用の最小値</param>
    /// <param name="maxValue">キャリブレーション用の最大値</param>
    /// <returns>キャリブレーションされた値</returns>
    private float Calibrate(int rawValue, int minValue, int maxValue)
    {
        // 値をクランプ（範囲外の値を修正）
        float clampedValue = Mathf.Clamp(rawValue, minValue, maxValue);

        // 最小値と最大値の差分で正規化し、0～100にスケーリング
        return ((clampedValue - minValue) / (maxValue - minValue)) * 100f;
    }
}
