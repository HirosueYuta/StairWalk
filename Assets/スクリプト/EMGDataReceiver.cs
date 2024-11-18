using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class EMGDataReceiver : MonoBehaviour
{
    public int port = 65432;
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private Thread receiveThread;
    private bool isRunning = true; // スレッド制御用フラグ

    [Header("EMG Data")]
    public int emgValue1; // 公開用のEMG値1
    public int emgValue2; // 公開用のEMG値2

    private int receivedEmgValue1; // 受信したEMG値1
    private int receivedEmgValue2; // 受信したEMG値2
    private readonly object dataLock = new object(); // 同期用ロックオブジェクト

    private int dataCount = 0; // 1秒間のデータ受信数
    private float timer = 0f;

    void Start()
    {
        // UDPクライアントの初期化
        udpClient = new UdpClient(port);
        remoteEndPoint = new IPEndPoint(IPAddress.Any, port);

        // データ受信スレッドを開始
        receiveThread = new Thread(ReceiveData)
        {
            IsBackground = true
        };
        receiveThread.Start();

        Debug.Log("Waiting for EMG data...");
    }

    void ReceiveData()
    {
        while (isRunning)
        {
            try
            {
                // データを受信
                byte[] data = udpClient.Receive(ref remoteEndPoint);

                if (data.Length >= 8) // int (4 bytes) * 2 = 8 bytes
                {
                    int value1 = BitConverter.ToInt32(data, 0); // 最初の4バイトを取得
                    int value2 = BitConverter.ToInt32(data, 4); // 次の4バイトを取得

                    // ロックしてデータを格納
                    lock (dataLock)
                    {
                        receivedEmgValue1 = value1;
                        receivedEmgValue2 = value2;
                        dataCount++;
                    }
                }
                else
                {
                    Debug.LogWarning("Received unexpected data length: " + data.Length);
                }
            }
            catch (SocketException e)
            {
                Debug.LogWarning("Socket error: " + e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError("Unexpected error: " + e.Message);
            }
        }
    }

    void Update()
    {
        // ロックして最新のデータを読み取り、公開フィールドに反映
        lock (dataLock)
        {
            emgValue1 = receivedEmgValue1;
            emgValue2 = receivedEmgValue2;
        }

        // 1秒ごとに受信データ件数をログ出力
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            int lastDataCount;

            // ロックして受信データ件数を読み取り＆リセット
            lock (dataLock)
            {
                lastDataCount = dataCount;
                dataCount = 0;
            }

            Debug.Log("Data received in last second: " + lastDataCount);
            timer = 0f;
        }
    }

    void OnDestroy()
    {
        // スレッド終了指示
        isRunning = false;

        // スレッドが終了するのを待つ
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join();
        }

        // ソケットを閉じる
        udpClient?.Close();
    }
}
