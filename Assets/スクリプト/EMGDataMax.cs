using UnityEngine;

public class EMGDataMax : MonoBehaviour
{
    public EMGDataReceiver dataReceiver; // EMGDataReceiverを参照
    [Header("Maximum and Minimum EMG Values")]
    public int maxEmgValue1; // EMGデータ1の最大値
    public int maxEmgValue2; // EMGデータ2の最大値
    public int minEmgValue1 = int.MaxValue; // EMGデータ1の最小値（初期値は最大可能値）
    public int minEmgValue2 = int.MaxValue; // EMGデータ2の最小値（初期値は最大可能値）


    void Update()
    {
        if (dataReceiver != null)
        {
            // EMGデータの現在値を取得
            int currentEmgValue1 = dataReceiver.emgValue1;
            int currentEmgValue2 = dataReceiver.emgValue2;

            // 最大値を更新
            if (currentEmgValue1 != 254 && currentEmgValue1 > maxEmgValue1)
            {
                maxEmgValue1 = currentEmgValue1;
            }

            if (currentEmgValue2 != 254 && currentEmgValue2 > maxEmgValue2)
            {
                maxEmgValue2 = currentEmgValue2;
            }

            // 最小値を更新
            if (currentEmgValue1 < minEmgValue1)
            {
                minEmgValue1 = currentEmgValue1;
            }

            if (currentEmgValue2 < minEmgValue2)
            {
                minEmgValue2 = currentEmgValue2;
            }
            Debug.Log("最大値：(" + maxEmgValue1 + "," + maxEmgValue2 + ")  最小値(" + minEmgValue1 + "," + minEmgValue2 + ")");
        }
        else
        {
            Debug.LogWarning("EMGDataReceiver reference is not set!");
        }
    }
}
