using UnityEngine;

public class ActionExecutor : MonoBehaviour
{
    public enum ActionType
    {
        Good,
        BadPublic,   // 必须被看到（player或object其中之一 seen）
        BadHidden    // 必须隐藏（player和object都 hidden，否则 seen 就抓）
    }

    [Header("References")]
    public BadnessMeter meter;
    public ArrestManager arrestManager;

    [Header("Tuning (default deltas)")]
    public int goodMinus = 10;            // -1格
    public int goodBigMinus = 20;         // -2格（捐钱）
    public int badPublicPlus = 20;        // +2格
    public int badHiddenPlus = 10;        // +1格

    /// <summary>
    /// customDelta:
    /// - pass 0 to use defaults above
    /// - pass a non-zero delta to override (e.g. donate -20)
    /// </summary>
    public bool TryExecute(
        ActionType type,
        VisibilityProbe2D playerProbe,
        VisibilityProbe2D targetProbe,
        int customDelta,
        bool showGoodFailHint, // (kept for compatibility; hint is handled outside)
        out string failReason,
        out bool arrested)
    {
        arrested = false;
        failReason = "";

        if (arrestManager != null && arrestManager.IsArrested)
        {
            failReason = "Already arrested.";
            arrested = true;
            return false;
        }

        bool playerSeen = (playerProbe != null) && playerProbe.IsSeen;
        bool targetSeen = (targetProbe != null) && targetProbe.IsSeen;

        bool anySeen = playerSeen || targetSeen;
        bool bothHidden = !playerSeen && !targetSeen;
        bool playerHidden = !playerSeen;

        int delta = customDelta;

        switch (type)
        {
            case ActionType.Good:
                {
                    // 好事：必须被看到才算成功
                    // 如果没有targetProbe（比如跳舞），只看 playerSeen
                    bool goodOk = (targetProbe == null) ? playerSeen : anySeen;

                    if (!goodOk)
                    {
                        failReason = "Good action must be done in NPC FOV.";
                        return false; // 不加不减
                    }

                    if (delta == 0) delta = -goodMinus; // 默认-10
                    if (meter != null) meter.Add(delta); // delta 为负就是减少
                    return true;
                }

            case ActionType.BadPublic:
                {
                    // 当面坏事：必须 anySeen
                    if (!anySeen)
                    {
                        failReason = "BadPublic requires being seen.";
                        return false; // 不加不减（也不提示）
                    }

                    if (delta == 0) delta = badPublicPlus; // 默认+20
                    if (meter != null) meter.Add(delta);
                    return true;
                }

            case ActionType.BadHidden:
                {
                    // 偷偷坏事：必须隐藏
                    // targetProbe == null（比如尿尿）：只要求 playerHidden
                    bool hiddenOk = (targetProbe == null) ? playerHidden : bothHidden;

                    if (!hiddenOk)
                    {
                        // 被看到 => 直接抓（等同坏蛋值满/进监狱）
                        if (anySeen)
                        {
                            arrested = true;
                            if (arrestManager != null)
                                arrestManager.Arrest("Caught doing a hidden bad action.");
                        }

                        failReason = "Hidden bad action failed.";
                        return false;
                    }

                    if (delta == 0) delta = badHiddenPlus; // 默认+10
                    if (meter != null) meter.Add(delta);
                    return true;
                }
        }

        failReason = "Unknown action.";
        return false;
    }
}