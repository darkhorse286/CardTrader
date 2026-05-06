namespace CardTrader.Authorization.Relations;

public static class FgaRelations
{
    // Shared across multiple types
    public const string Owner = "owner";
    public const string Viewer = "viewer";
    public const string CanView = "can_view";
    public const string CanManage = "can_manage";

    // card_instance
    public const string Collection = "collection";
    public const string CanTrade = "can_trade";

    // trade_proposal
    public const string Initiator = "initiator";
    public const string Recipient = "recipient";
    public const string Supervisor = "supervisor";
    public const string Facilitator = "facilitator";
    public const string CanAccept = "can_accept";
    public const string CanCancel = "can_cancel";

    // delegation
    public const string Delegator = "delegator";
    public const string Delegatee = "delegatee";
    public const string ActiveDelegator = "active_delegator";
}
