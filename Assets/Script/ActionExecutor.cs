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

    [Header("Tuning")]
    public int goodMinus = 10;            // -1格
    public int goodBigMinus = 20;         // -2格（捐钱）
    public int badPublicPlus = 20;        // +2格
    public int badHiddenPlus = 10;        // +1格

    public bool TryExecute(
        ActionType type,
        VisibilityProbe2D playerProbe,
        VisibilityProbe2D targetProbe,
        int customDelta,
        bool showGoodFailHint,
        out string failReason,
        out bool arrested)
    {
        arrested = false;
        failReason = "";

        if (arrestManager != null && arrestManager.IsArrested)
        {
            failReason = "Already arrested.";
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
                // 好事：玩家或物体至少一个要被看到（如果没有targetProbe，视为只看玩家）
                bool goodOk = (targetProbe == null) ? playerSeen : anySeen;
                if (!goodOk)
                {
                    failReason = "Good action must be done in NPC FOV.";
                    return false;
                }

                if (delta == 0) delta = -goodMinus;
                if (meter != null) meter.Add(delta); // delta 是负数表示减少
                return true;

            case ActionType.BadPublic:
                // 当面坏事：按/点瞬间必须 anySeen
                if (!anySeen)
                {
                    failReason = "BadPublic requires being seen (no hint by design).";
                    return false;
                }

                if (delta == 0) delta = badPublicPlus;
                if (meter != null) meter.Add(delta);
                return true;

            case ActionType.BadHidden:
                // 偷偷坏事：必须隐藏；如果被看到 -> 直接抓（等同满条）
                // 注意：如果 targetProbe == null（比如尿尿）就只要求 playerHidden
                bool hiddenOk = (targetProbe == null) ? playerHidden : bothHidden;

                if (!hiddenOk)
                {
                    // 被看到就抓
                    if (anySeen)
                    {
                        arrested = true;
                        if (arrestManager != null) arrestManager.Arrest("Caught doing a hidden bad action.");
                    }
                    failReason = "Hidden bad action failed.";
                    return false;
                }

                if (delta == 0) delta = badHiddenPlus;
                if (meter != null) meter.Add(delta);
                return true;
        }

        failReason = "Unknown action.";
        return false;
    }
}