using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class SteamVRRecenter : MonoBehaviour
{
    [Tooltip("再センターを実行したいときにチェック")]
    public bool doRecenter = false;

    void Update()
    {
        // 例：Rキーでも呼べるように
        if (doRecenter || Input.GetKeyDown(KeyCode.R))
        {
            Recenter();
            doRecenter = false;
        }
    }

    void Recenter()
    {
        // プロジェクトで使われている全てのXRInputSubsystemを取得
        List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetInstances(subsystems);

        if (subsystems.Count == 0)
        {
            Debug.LogWarning("XRInputSubsystem が見つかりませんでした。");
            return;
        }

        // それぞれで TryRecenter() を呼ぶ
        foreach (var xr in subsystems)
        {
            bool ok = xr.TryRecenter();
            Debug.Log($"Recenter on {xr.GetType().Name}: {ok}");
        }
    }
}
