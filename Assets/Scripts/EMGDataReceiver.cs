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

    [Header("EMG Data")]
    public int emgValue1;
    public int emgValue2;

    private int receivedEmgValue1;
    private int receivedEmgValue2;
    private readonly object dataLock = new object(); // 同期用のロックオブジェクト

    private int dataCount = 0;
    private float timer = 0f;

    void Start()
    {
        udpClient = new UdpClient(port);
        remoteEndPoint = new IPEndPoint(IPAddress.Any, port);

        // 受信スレッドの開始
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        Debug.Log("Waiting for EMG data...");
    }

    void ReceiveData()
    {
        while (true)
        {
            try
            {
                // データの受信
                byte[] data = udpClient.Receive(ref remoteEndPoint);

                if (data.Length >= 8) // int (4 bytes) * 2 = 8 bytes
                {
                    int value1 = BitConverter.ToInt32(data, 0); // 最初の4バイトを取得
                    int value2 = BitConverter.ToInt32(data, 4); // 次の4バイトを取得

                    // 受信したデータをロックして格納
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
            catch (Exception e)
            {
                Debug.LogError("Error receiving EMG data: " + e.Message);
                break;
            }
        }
    }

    void Update()
    {
        // ロックしてデータを読み取り、公開フィールドに反映
        lock (dataLock)
        {
            emgValue1 = receivedEmgValue1;
            emgValue2 = receivedEmgValue2;
        }

        // 1秒ごとに受信したデータ数を表示
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            Debug.Log("Data received in last second: " + dataCount);
            dataCount = 0;
            timer = 0f;
        }
    }

    void OnDestroy()
    {
        // スレッドの終了とソケットのクローズ
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        udpClient?.Close();
    }
}
