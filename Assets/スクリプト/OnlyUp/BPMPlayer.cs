using UnityEngine;

public class BPMPlayer : MonoBehaviour
{
    public AudioClip bpm; 
    private AudioSource audioSource;
    public float interval = 0.8f;   // 音を鳴らす間隔 (秒)

    private float timer;

    void Start()
    {
        GameObject bpmAudioObject = new GameObject("BPMAudioSource");
        bpmAudioObject.transform.parent = this.transform;
        audioSource = bpmAudioObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        timer = interval; // 初回実行のためタイマーを初期化
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            PlaySound();
            timer = interval; // タイマーをリセット
        }
    }

    void PlaySound()
    {
        if (audioSource != null)
        {
            audioSource.PlayOneShot(bpm);
        }
    }
}