using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class HeightRecorder : MonoBehaviour
{
    public Transform object1;  // 記録するオブジェクト1のTransform
    public Transform object2;  // 記録するオブジェクト2のTransform
    public Transform object3;  // 記録するオブジェクト3のTransform

    private List<float> object1Heights = new List<float>();  // オブジェクト1の高さリスト
    private List<float> object2Heights = new List<float>();  // オブジェクト2の高さリスト
    private List<float> object3Heights = new List<float>();  // オブジェクト3の高さリスト
    private List<float> timeStamps = new List<float>();      // 時間の記録リスト

    void Update()
    {
        // 各オブジェクトのY座標（高さ）をリストに記録
        object1Heights.Add(object1.position.y);
        object2Heights.Add(object2.position.y);
        object3Heights.Add(object3.position.y);
        timeStamps.Add(Time.time);  // 経過時間を記録

        // Qキーを押したときにデータをCSVに保存
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SaveHeightsToCSV();
        }
    }

    void SaveHeightsToCSV()
    {
        // ファイルパスの設定（プロジェクトの Assets フォルダに保存されます）
        string path = Application.dataPath + "/HeightData.csv";
        
        // CSVファイルに書き込み
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("Time,Object1Height,Object2Height,Object3Height");

            for (int i = 0; i < object1Heights.Count; i++)
            {
                string line = $"{timeStamps[i]},{object1Heights[i]},{object2Heights[i]},{object3Heights[i]}";
                writer.WriteLine(line);
            }
        }

        Debug.Log("Height data saved to " + path);
    }
}