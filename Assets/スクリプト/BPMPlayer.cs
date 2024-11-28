using UnityEngine;

public class BPMPlayer : MonoBehaviour
{
    public AudioSource audioSource; // 再生するAudioSource
    public float bpm = 75f;         // BPMの値

    private float interval;         // ビート間隔
    private float timer = 0f;       // タイマー
    private int count = 0;

    void Start()
    {
        // ビート間隔を計算
        interval = 60f / bpm;
    }

    void Update()
    {
        // タイマーを更新
        timer += Time.deltaTime;

        // ビート間隔を超えた場合に音を再生
        if (timer >= interval)
        {
            audioSource.Stop(); // 再生を停止（これで位置がリセットされる）
            audioSource.Play(); // 音を再生
            count++;
            //Debug.Log("click"+count+"回目");
            timer -= interval; // タイマーをリセット
        }
    }
}
