using UnityEngine;

public class PositionLogger : MonoBehaviour
{
    [Tooltip("位置をログ出力したいオブジェクト（最大３つまで）を指定")]
    public Transform[] targetObjects = new Transform[3];

    [Header("ログ出力タイミング")]
    public bool logOnStart = true;      // Start() で一度だけ出力する
    public bool logEveryFrame = false;  // Update() で毎フレーム出力する
    public bool logInterval = false;    // 一定間隔で出力する
    [Tooltip("logInterval が true のときの出力間隔（秒）")]
    public float intervalSeconds = 1.0f;

    float _timer = 0f;

    void Start()
    {
        if (logOnStart) LogPositions("Start");
    }

    void Update()
    {
        if (logEveryFrame) LogPositions("Update");

        if (logInterval)
        {
            _timer += Time.deltaTime;
            if (_timer >= intervalSeconds)
            {
                LogPositions($"Interval ({intervalSeconds:f2}s)");
                _timer = 0f;
            }
        }
    }

    void LogPositions(string when)
    {
        for (int i = 0; i < targetObjects.Length; i++)
        {
            var target = targetObjects[i];
            if (target == null) continue;

            Vector3 pos = target.position;
            Debug.Log($"[{when}] {target.name} の位置: ({pos.x:F3}, {pos.y:F003}, {pos.z:F003})");
        }
    }
}
