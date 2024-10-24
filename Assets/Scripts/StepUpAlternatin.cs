using UnityEngine;

public class StepUpAlternating : MonoBehaviour
{
    public Transform rightShoe;  // 右靴のTransform
    public Transform leftShoe;   // 左靴のTransform
    public float stepHeight = 0.2f;  // 階段の高さ
    public float stepDepth = 0.4f;   // 階段の幅
    public float stepSpeed = 1.0f;   // 階段を登る速度
    public float curveStrength = 1.0f;  // 曲線のカーブの強さを調整する係数

    private bool isStepping = false; // 階段を登っている最中かどうか
    private bool isRightShoeTurn = true; // どちらの靴が次に動くかのフラグ
    private bool isFirstStep = true;  // 最初のステップかどうか
    private float progress = 0.0f;  // 移動の進行状況を管理
    private Vector3 startPosition;  // 移動の開始位置
    private Vector3 targetPosition; // 移動の目標位置

    void Update()
    {
        // 入力をチェック (例: スペースキー)
        if (Input.GetKeyDown(KeyCode.Space) && !isStepping)
        {
            isStepping = true;
            progress = 0.0f;

            float heightMultiplier = isFirstStep ? 1.0f : 2.0f;  // 最初のステップなら1段、それ以降は2段分
            float depthMultiplier = isFirstStep ? 1.0f : 2.0f;

            if (isRightShoeTurn)
            {
                startPosition = rightShoe.position;
                targetPosition = rightShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
            }
            else
            {
                startPosition = leftShoe.position;
                targetPosition = leftShoe.position + new Vector3(0, stepHeight * heightMultiplier, stepDepth * depthMultiplier);
            }
        }

        // 曲線移動を行う
        if (isStepping)
        {
            progress += stepSpeed * Time.deltaTime;
            progress = Mathf.Clamp01(progress); // 進行状況が0〜1の範囲内になるようにする

            // 現在の進行に基づいてLerpで移動位置を計算
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, progress);

            // 曲線を描くようにy軸を持ち上げる
            currentPosition.y += Mathf.Sin(progress * Mathf.PI) * stepHeight * curveStrength;

            if (isRightShoeTurn)
            {
                rightShoe.position = currentPosition;
            }
            else
            {
                leftShoe.position = currentPosition;
            }

            // 移動が完了したらステップを終了
            if (progress >= 1.0f)
            {
                isStepping = false;
                isRightShoeTurn = !isRightShoeTurn; // 次の靴に切り替える
                isFirstStep = false;  // 最初のステップが完了したことをマーク
            }
        }
    }
}
