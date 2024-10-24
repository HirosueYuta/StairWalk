using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class EMGDataReceiver : MonoBehaviour
{
    UdpClient udpClient;  // UDPクライアント
    IPEndPoint remoteEndPoint;

    // 送られてきた筋電データをインスペクターで表示
    [Header("EMG Data")]
    public float emgValue1;
    public float emgValue2;

    // 1秒間に受信したデータの数を表示するための変数
    private int dataCount = 0;
    private float timer = 0f;

    void Start()
    {
        // UDPクライアントの初期化
        udpClient = new UdpClient(65432);  // ポート65432で受信
        remoteEndPoint = new IPEndPoint(IPAddress.Any, 65432);
        Debug.Log("Waiting for EMG data...");
    }

    void Update()
    {
        // データが届いているかを確認
        if (udpClient.Available > 0)
        {
            try
            {
                // データ受信
                byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                string receivedData = Encoding.UTF8.GetString(receivedBytes);

                // カンマで区切られた筋電データを取得
                string[] emgValues = receivedData.Split(',');

                if (emgValues.Length >= 2)
                {
                    // 受信データを解析してインスペクターに表示
                    emgValue1 = float.Parse(emgValues[0]);
                    emgValue2 = float.Parse(emgValues[1]);

                    // デバッグ用出力
                    //Debug.Log("Received EMG Values - EMG1: " + emgValue1 + ", EMG2: " + emgValue2);

                    // データ数をカウント
                    dataCount++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error receiving EMG data: " + e.Message);
            }
        }

        // 1秒間に受信したデータ数をコンソールに出力
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            Debug.Log("Data received in last second: " + dataCount);
            dataCount = 0;  // カウントリセット
            timer = 0f;     // タイマーリセット
        }
    }

    void OnApplicationQuit()
    {
        // アプリケーション終了時にクライアントをクローズ
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}
